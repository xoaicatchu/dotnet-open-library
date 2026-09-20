using System.Net;
using System.Net.Http.Json;
using LinqToDB;
using LinqToDB.AspNet;
using LinqToDB.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Api.Data;
using OrderManagement.Api.Models;

namespace OrderManagement.Tests;

public class OrderTestsFixture : WebApplicationFactory<Program>
{
    private readonly string _dbPath;

    public OrderTestsFixture()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"orders_test_{Guid.NewGuid():N}.db");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptors = services.Where(d =>
                d.ServiceType == typeof(AppDataConnection) ||
                d.ServiceType == typeof(DataOptions<AppDataConnection>)).ToList();
            foreach (var d in descriptors)
            {
                services.Remove(d);
            }

            var connStr = $"Data Source={_dbPath}";
            services.AddLinqToDBContext<AppDataConnection>((_, options) =>
                options.UseSQLite(connStr));

            using var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDataConnection>();
            DbInitializer.Initialize(db);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (File.Exists(_dbPath))
        {
            try { File.Delete(_dbPath); } catch { }
        }
    }
}

public class OrderTests : IClassFixture<OrderTestsFixture>
{
    private readonly HttpClient _client;

    public OrderTests(OrderTestsFixture fixture)
    {
        _client = fixture.CreateClient();
    }

    [Fact]
    public async Task GetAll_ReturnsSeededOrders()
    {
        var response = await _client.GetAsync("/api/orders");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var orders = await response.Content.ReadFromJsonAsync<List<OrderSummaryDto>>();
        Assert.NotNull(orders);
        Assert.NotEmpty(orders);
        Assert.All(orders, o => Assert.True(o.ItemCount > 0));
    }

    [Fact]
    public async Task GetAll_WithStatusFilter_ReturnsMatchingOrders()
    {
        var response = await _client.GetAsync("/api/orders?status=Processing");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var orders = await response.Content.ReadFromJsonAsync<List<OrderSummaryDto>>();
        Assert.NotNull(orders);
        Assert.All(orders, o => Assert.Equal("Processing", o.Status));
    }

    [Fact]
    public async Task GetAll_WithSearchKeyword_ReturnsMatchingOrders()
    {
        var response = await _client.GetAsync("/api/orders?search=Alice");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var orders = await response.Content.ReadFromJsonAsync<List<OrderSummaryDto>>();
        Assert.NotNull(orders);
        Assert.Contains(orders, o => o.CustomerName.Contains("Alice"));
    }

    [Fact]
    public async Task GetById_WithValidId_ReturnsOrderAndItems()
    {
        var response = await _client.GetAsync("/api/orders/1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var order = await response.Content.ReadFromJsonAsync<OrderDetailDto>();
        Assert.NotNull(order);
        Assert.Equal(1, order.Id);
        Assert.NotEmpty(order.Items);
    }

    [Fact]
    public async Task GetById_WithNonExistentId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/orders/9999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetStatistics_ReturnsCorrectAggregates()
    {
        var response = await _client.GetAsync("/api/orders/statistics");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var stats = await response.Content.ReadFromJsonAsync<OrderStatisticsDto>();
        Assert.NotNull(stats);
        Assert.True(stats.TotalOrders > 0);
        Assert.True(stats.TotalRevenue > 0);
        Assert.True(stats.TotalItemsSold > 0);
        Assert.NotEmpty(stats.OrdersByStatus);
    }

    [Fact]
    public async Task Create_WithValidData_ReturnsCreatedAndItems()
    {
        var createDto = new CreateOrderDto(
            CustomerName: "John Doe",
            ShippingAddress: "100 Broadway, NY",
            Items: new List<CreateOrderItemDto>
            {
                new("Wireless Earbuds", 1, 79.99m),
                new("Silicone Case", 2, 9.99m)
            }
        );

        var response = await _client.PostAsJsonAsync("/api/orders", createDto);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<OrderDetailDto>();
        Assert.NotNull(created);
        Assert.Equal("John Doe", created.CustomerName);
        Assert.Equal(99.97m, created.TotalAmount);
        Assert.Equal(2, created.Items.Count);
    }

    [Fact]
    public async Task Create_WithEmptyCustomer_ReturnsBadRequest()
    {
        var createDto = new CreateOrderDto(
            CustomerName: "",
            ShippingAddress: "100 Broadway, NY",
            Items: new List<CreateOrderItemDto>
            {
                new("Item", 1, 10m)
            }
        );

        var response = await _client.PostAsJsonAsync("/api/orders", createDto);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithEmptyItems_ReturnsBadRequest()
    {
        var createDto = new CreateOrderDto(
            CustomerName: "Empty Items Buyer",
            ShippingAddress: "100 Broadway, NY",
            Items: new List<CreateOrderItemDto>()
        );

        var response = await _client.PostAsJsonAsync("/api/orders", createDto);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_WithValidStatus_ReturnsUpdatedOrder()
    {
        var updateDto = new UpdateOrderStatusDto("Completed");
        var response = await _client.PatchAsJsonAsync("/api/orders/1/status", updateDto);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await response.Content.ReadFromJsonAsync<OrderSummaryDto>();
        Assert.NotNull(updated);
        Assert.Equal("Completed", updated.Status);
        Assert.NotNull(updated.UpdatedAt);
    }

    [Fact]
    public async Task UpdateStatus_WithNonExistentId_ReturnsNotFound()
    {
        var updateDto = new UpdateOrderStatusDto("Completed");
        var response = await _client.PatchAsJsonAsync("/api/orders/8888/status", updateDto);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WithValidId_RemovesOrderAndItems()
    {
        // First create an order to delete
        var createDto = new CreateOrderDto(
            CustomerName: "Temp Delete",
            ShippingAddress: "Temp Addr",
            Items: new List<CreateOrderItemDto> { new("Temp Item", 1, 5m) }
        );
        var createRes = await _client.PostAsJsonAsync("/api/orders", createDto);
        var created = await createRes.Content.ReadFromJsonAsync<OrderDetailDto>();
        Assert.NotNull(created);

        var deleteRes = await _client.DeleteAsync($"/api/orders/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteRes.StatusCode);

        var getRes = await _client.GetAsync($"/api/orders/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getRes.StatusCode);
    }

    [Fact]
    public async Task Delete_WithNonExistentId_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync("/api/orders/9999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
