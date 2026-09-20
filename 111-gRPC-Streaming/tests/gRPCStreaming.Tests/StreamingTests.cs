using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using gRPCStreaming.Shared;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace gRPCStreaming.Tests;

public class StreamingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly GrpcChannel _channel;

    public StreamingTests(WebApplicationFactory<Program> factory)
    {
        _channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions
        {
            HttpHandler = factory.Server.CreateHandler()
        });
    }

    // 1. Unary: GetProduct(id=1) -> returns product name="iPhone 15"
    [Fact]
    public async Task Unary_GetProduct_ReturnsProduct()
    {
        var client = new ProductStream.ProductStreamClient(_channel);
        var reply = await client.GetProductAsync(new GetProductRequest { Id = 1 });
        reply.Name.Should().Be("iPhone 15");
    }

    // 2. Unary: GetProduct(id=999) -> RpcException StatusCode.NotFound
    [Fact]
    public async Task Unary_GetProduct_NotFound_ThrowsRpcException()
    {
        var client = new ProductStream.ProductStreamClient(_channel);
        var ex = await Assert.ThrowsAsync<RpcException>(() => client.GetProductAsync(new GetProductRequest { Id = 999 }).ResponseAsync);
        ex.StatusCode.Should().Be(StatusCode.NotFound);
    }

    // 3. Server Streaming: StreamPrices(product_id=1, count=3) -> nhận được 3 price updates
    [Fact]
    public async Task ServerStreaming_StreamPrices_ReturnsStream()
    {
        var client = new ProductStream.ProductStreamClient(_channel);
        using var call = client.StreamPrices(new StreamPricesRequest { ProductId = 1, Count = 3 });
        
        var updates = new List<PriceUpdate>();
        await foreach (var update in call.ResponseStream.ReadAllAsync())
        {
            updates.Add(update);
        }
        
        updates.Count.Should().Be(3);
    }

    // 4. Server Streaming: ListProducts(page_size=4) -> nhận được 4 products
    [Fact]
    public async Task ServerStreaming_ListProducts_ReturnsCorrectAmount()
    {
        var client = new ProductStream.ProductStreamClient(_channel);
        using var call = client.ListProducts(new ListProductsRequest { PageSize = 4 });
        
        var products = new List<ProductReply>();
        await foreach (var product in call.ResponseStream.ReadAllAsync())
        {
            products.Add(product);
        }
        
        products.Count.Should().Be(4);
    }

    // 5. Server Streaming: values hợp lệ (price > 0, timestamp không empty)
    [Fact]
    public async Task ServerStreaming_StreamPrices_HasValidData()
    {
        var client = new ProductStream.ProductStreamClient(_channel);
        using var call = client.StreamPrices(new StreamPricesRequest { ProductId = 1, Count = 1 });
        
        var hasData = await call.ResponseStream.MoveNext(CancellationToken.None);
        hasData.Should().BeTrue();
        
        var update = call.ResponseStream.Current;
        update.Price.Should().BeGreaterThan(0);
        update.Timestamp.Should().NotBeNullOrEmpty();
    }

    // 6. Client Streaming: gửi 5 products -> UploadSummary.TotalReceived = 5
    [Fact]
    public async Task ClientStreaming_UploadProducts_ReturnsSummary()
    {
        var client = new BulkUpload.BulkUploadClient(_channel);
        using var call = client.UploadProducts();
        
        for (int i = 0; i < 5; i++)
        {
            await call.RequestStream.WriteAsync(new ProductUploadRequest { Name = $"Product {i}", Price = 10, Stock = 5 });
        }
        await call.RequestStream.CompleteAsync();
        
        var summary = await call.ResponseAsync;
        summary.TotalReceived.Should().Be(5);
        summary.TotalSaved.Should().Be(5);
    }

    // 7. Client Streaming: gửi mix valid/invalid -> TotalSaved < TotalReceived
    [Fact]
    public async Task ClientStreaming_UploadProducts_FiltersInvalid()
    {
        var client = new BulkUpload.BulkUploadClient(_channel);
        using var call = client.UploadProducts();
        
        await call.RequestStream.WriteAsync(new ProductUploadRequest { Name = "Valid", Price = 10 });
        await call.RequestStream.WriteAsync(new ProductUploadRequest { Name = "", Price = 10 }); // invalid
        await call.RequestStream.CompleteAsync();
        
        var summary = await call.ResponseAsync;
        summary.TotalReceived.Should().Be(2);
        summary.TotalSaved.Should().Be(1);
    }

    // 8. Client Streaming: gửi 0 records -> TotalReceived = 0
    [Fact]
    public async Task ClientStreaming_UploadProducts_EmptyStream()
    {
        var client = new BulkUpload.BulkUploadClient(_channel);
        using var call = client.UploadProducts();
        await call.RequestStream.CompleteAsync();
        
        var summary = await call.ResponseAsync;
        summary.TotalReceived.Should().Be(0);
        summary.TotalSaved.Should().Be(0);
    }

    // 9. Bidirectional: gửi 3 messages -> nhận được 3 echo responses
    [Fact]
    public async Task Bidirectional_Connect_ReturnsEchoes()
    {
        var client = new Chat.ChatClient(_channel);
        using var call = client.Connect();
        
        var readTask = Task.Run(async () =>
        {
            var messages = new List<ChatMessage>();
            await foreach (var msg in call.ResponseStream.ReadAllAsync())
            {
                messages.Add(msg);
            }
            return messages;
        });

        for (int i = 0; i < 3; i++)
        {
            await call.RequestStream.WriteAsync(new ChatMessage { User = "User", Message = $"Msg {i}" });
        }
        await call.RequestStream.CompleteAsync();
        
        var responses = await readTask;
        responses.Count.Should().Be(3);
    }

    // 10. Bidirectional: echo response chứa original message trong nội dung
    [Fact]
    public async Task Bidirectional_Connect_EchoContainsOriginal()
    {
        var client = new Chat.ChatClient(_channel);
        using var call = client.Connect();
        
        await call.RequestStream.WriteAsync(new ChatMessage { User = "TestUser", Message = "Hello World" });
        await call.RequestStream.CompleteAsync();
        
        var hasData = await call.ResponseStream.MoveNext(CancellationToken.None);
        hasData.Should().BeTrue();
        
        var response = call.ResponseStream.Current;
        response.Message.Should().Contain("TestUser");
        response.Message.Should().Contain("Hello World");
    }
}
