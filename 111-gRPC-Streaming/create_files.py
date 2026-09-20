import os

base_dir = r"d:\GitHub\dotnet-example\111-gRPC-Streaming"

files = {
    "gRPCStreaming.slnx": \"\"\"<Solution>
  <Folder Name="/src/">
    <Project Path="src/Shared.Protos/Shared.Protos.csproj" />
    <Project Path="src/gRPCStreaming.Server/gRPCStreaming.Server.csproj" />
  </Folder>
  <Folder Name="/tests/">
    <Project Path="tests/gRPCStreaming.Tests/gRPCStreaming.Tests.csproj" />
  </Folder>
</Solution>\"\"\",
    "src/Shared.Protos/Shared.Protos.csproj": \"\"\"<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Google.Protobuf" Version="3.30.2" />
    <PackageReference Include="Grpc.Net.Client" Version="2.67.0" />
    <PackageReference Include="Grpc.Tools" Version="2.70.0" PrivateAssets="All" />
  </ItemGroup>

  <ItemGroup>
    <Protobuf Include="Protos\\\\product.proto" GrpcServices="Both" />
    <Protobuf Include="Protos\\\\upload.proto" GrpcServices="Both" />
    <Protobuf Include="Protos\\\\chat.proto" GrpcServices="Both" />
  </ItemGroup>
</Project>\"\"\",
    "src/Shared.Protos/Protos/product.proto": \"\"\"syntax = "proto3";
option csharp_namespace = "gRPCStreaming.Shared";
package product;

service ProductStream {
  // Unary
  rpc GetProduct (GetProductRequest) returns (ProductReply);
  // Server Streaming: client request 1 l?n, server stream nhi?u response
  rpc StreamPrices (StreamPricesRequest) returns (stream PriceUpdate);
  // Server Streaming: stream all products
  rpc ListProducts (ListProductsRequest) returns (stream ProductReply);
}

message GetProductRequest { int32 id = 1; }
message ProductReply { int32 id = 1; string name = 2; double price = 3; int32 stock = 4; }
message StreamPricesRequest { int32 product_id = 1; int32 count = 2; }
message PriceUpdate { int32 product_id = 1; double price = 2; string timestamp = 3; }
message ListProductsRequest { int32 page_size = 1; }\"\"\",
    "src/Shared.Protos/Protos/upload.proto": \"\"\"syntax = "proto3";
option csharp_namespace = "gRPCStreaming.Shared";
package upload;

service BulkUpload {
  // Client Streaming: client g?i nhi?u records, server tr? v? 1 summary
  rpc UploadProducts (stream ProductUploadRequest) returns (UploadSummary);
}

message ProductUploadRequest { string name = 1; double price = 2; int32 stock = 3; }
message UploadSummary { int32 total_received = 1; int32 total_saved = 2; string message = 3; }\"\"\",
    "src/Shared.Protos/Protos/chat.proto": \"\"\"syntax = "proto3";
option csharp_namespace = "gRPCStreaming.Shared";
package chat;

service Chat {
  // Bidirectional: c? 2 phía d?u stream
  rpc Connect (stream ChatMessage) returns (stream ChatMessage);
}

message ChatMessage { string user = 1; string message = 2; string timestamp = 3; }\"\"\",
    "src/gRPCStreaming.Server/gRPCStreaming.Server.csproj": \"\"\"<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Grpc.AspNetCore" Version="2.70.0" />
    <PackageReference Include="Grpc.AspNetCore.Server.Reflection" Version="2.70.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\\\\Shared.Protos\\\\Shared.Protos.csproj" />
  </ItemGroup>
</Project>\"\"\",
    "src/gRPCStreaming.Server/Program.cs": \"\"\"using gRPCStreaming.Server.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGrpc(options => {
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.MaxReceiveMessageSize = 16 * 1024 * 1024; // 16MB
    options.MaxSendMessageSize = 16 * 1024 * 1024;
});
builder.Services.AddGrpcReflection();

var app = builder.Build();

app.MapGrpcReflectionService();
app.MapGrpcService<ProductStreamService>();
app.MapGrpcService<UploadStreamService>();
app.MapGrpcService<ChatStreamService>();
app.MapGet("/", () => "gRPC Streaming Server running. Use gRPC client to connect.");

app.Run();

public partial class Program { }
\"\"\",
    "src/gRPCStreaming.Server/appsettings.json": \"\"\"{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}\"\"\",
    "src/gRPCStreaming.Server/Properties/launchSettings.json": \"\"\"{
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "http://localhost:5321",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "grpc": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "http://localhost:5311",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}\"\"\",
    "src/gRPCStreaming.Server/Services/ProductStreamService.cs": \"\"\"using Grpc.Core;
using gRPCStreaming.Shared;

namespace gRPCStreaming.Server.Services;

public class ProductStreamService : ProductStream.ProductStreamBase
{
    private static readonly List<(int Id, string Name, double Price, int Stock)> _products = [
        (1, "iPhone 15", 999.99, 50), (2, "MacBook Pro", 2499.99, 20),
        (3, "iPad Air", 599.99, 100), (4, "AirPods Pro", 249.99, 200)
    ];
    
    public override Task<ProductReply> GetProduct(GetProductRequest request, ServerCallContext context)
    {
        var p = _products.FirstOrDefault(p => p.Id == request.Id);
        if (p == default)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Product {request.Id} not found"));
        }
        return Task.FromResult(new ProductReply { Id = p.Id, Name = p.Name, Price = p.Price, Stock = p.Stock });
    }
    
    public override async Task StreamPrices(
        StreamPricesRequest request,
        IServerStreamWriter<PriceUpdate> responseStream,
        ServerCallContext context)
    {
        var rng = new Random();
        var count = Math.Min(request.Count, 10);
        for (int i = 0; i < count && !context.CancellationToken.IsCancellationRequested; i++)
        {
            var p = _products.FirstOrDefault(p => p.Id == request.ProductId);
            var basePrice = p != default ? p.Price : 100.0;
            var fluctuation = (rng.NextDouble() - 0.5) * 10;
            await responseStream.WriteAsync(new PriceUpdate {
                ProductId = request.ProductId,
                Price = Math.Round(basePrice + fluctuation, 2),
                Timestamp = DateTime.UtcNow.ToString("O")
            });
            await Task.Delay(100, context.CancellationToken);
        }
    }
    
    public override async Task ListProducts(
        ListProductsRequest request,
        IServerStreamWriter<ProductReply> responseStream,
        ServerCallContext context)
    {
        var pageSize = request.PageSize > 0 ? request.PageSize : 10;
        foreach (var p in _products.Take(pageSize))
        {
            if (context.CancellationToken.IsCancellationRequested) break;
            await responseStream.WriteAsync(
                new ProductReply { Id = p.Id, Name = p.Name, Price = p.Price, Stock = p.Stock });
            await Task.Delay(50, context.CancellationToken);
        }
    }
}\"\"\",
    "src/gRPCStreaming.Server/Services/UploadStreamService.cs": \"\"\"using Grpc.Core;
using gRPCStreaming.Shared;

namespace gRPCStreaming.Server.Services;

public class UploadStreamService : BulkUpload.BulkUploadBase
{
    public override async Task<UploadSummary> UploadProducts(
        IAsyncStreamReader<ProductUploadRequest> requestStream,
        ServerCallContext context)
    {
        var received = 0;
        var saved = new List<string>();
        
        await foreach (var request in requestStream.ReadAllAsync(context.CancellationToken))
        {
            received++;
            if (!string.IsNullOrEmpty(request.Name) && request.Price > 0)
                saved.Add(request.Name);
        }
        
        return new UploadSummary {
            TotalReceived = received,
            TotalSaved = saved.Count,
            Message = $"Upload complete. Saved: {string.Join(", ", saved.Take(3))}"
        };
    }
}\"\"\",
    "src/gRPCStreaming.Server/Services/ChatStreamService.cs": \"\"\"using Grpc.Core;
using gRPCStreaming.Shared;

namespace gRPCStreaming.Server.Services;

public class ChatStreamService : Chat.ChatBase
{
    public override async Task Connect(
        IAsyncStreamReader<ChatMessage> requestStream,
        IServerStreamWriter<ChatMessage> responseStream,
        ServerCallContext context)
    {
        await foreach (var msg in requestStream.ReadAllAsync(context.CancellationToken))
        {
            await responseStream.WriteAsync(new ChatMessage {
                User = "Server",
                Message = $"[Echo] {msg.User}: {msg.Message}",
                Timestamp = DateTime.UtcNow.ToString("O")
            });
        }
    }
}\"\"\",
    "tests/gRPCStreaming.Tests/gRPCStreaming.Tests.csproj": \"\"\"<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Grpc.AspNetCore.Server.ClientFactory" Version="2.70.0" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.12" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.0" />
    <PackageReference Include="FluentAssertions" Version="8.3.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\\\\..\\\\src\\\\gRPCStreaming.Server\\\\gRPCStreaming.Server.csproj" />
  </ItemGroup>
</Project>\"\"\",
    "tests/gRPCStreaming.Tests/StreamingTests.cs": \"\"\"using FluentAssertions;
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

    // 3. Server Streaming: StreamPrices(product_id=1, count=3) -> nh?n du?c 3 price updates
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

    // 4. Server Streaming: ListProducts(page_size=4) -> nh?n du?c 4 products
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

    // 5. Server Streaming: values h?p l? (price > 0, timestamp không empty)
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

    // 6. Client Streaming: g?i 5 products -> UploadSummary.TotalReceived = 5
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

    // 7. Client Streaming: g?i mix valid/invalid -> TotalSaved < TotalReceived
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

    // 8. Client Streaming: g?i 0 records -> TotalReceived = 0
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

    // 9. Bidirectional: g?i 3 messages -> nh?n du?c 3 echo responses
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

    // 10. Bidirectional: echo response ch?a original message trong n?i dung
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
}\"\"\"
}

for rel_path, content in files.items():
    abs_path = os.path.join(base_dir, rel_path)
    os.makedirs(os.path.dirname(abs_path), exist_ok=True)
    with open(abs_path, 'w', encoding='utf-8') as f:
        f.write(content)
