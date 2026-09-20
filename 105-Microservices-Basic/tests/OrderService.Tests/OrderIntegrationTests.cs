using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Data;
using OrderService.Entities;
using OrderService.Infrastructure;
using System.Net.Http.Json;
using Xunit;

namespace OrderService.Tests;

public class OrderIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public OrderIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var grpcDesc = services.SingleOrDefault(d => d.ServiceType == typeof(IProductServiceClient));
                if (grpcDesc != null) services.Remove(grpcDesc);
                services.AddScoped<IProductServiceClient, MockProductServiceClient>();

                var dbDesc = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<OrderDbContext>));
                if (dbDesc != null) services.Remove(dbDesc);
                
                services.AddDbContext<OrderDbContext>(opt => opt.UseSqlite("Data Source=test_orders.db"));
                
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
                db.Orders.Add(new OrderEntity { Id = 1, CreatedAt = DateTime.UtcNow });
                db.SaveChanges();
            });
        });
    }

    [Fact]
    public async Task GetOrders_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/orders");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CreateOrder_ValidProduct_ReturnsCreated()
    {
        var client = _factory.CreateClient();
        // Quantity <= 10 mock trả về success
        var response = await client.PostAsJsonAsync("/api/orders", new { ProductId = 1, Quantity = 5 });
        response.EnsureSuccessStatusCode();
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_OutOfStock_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        // Quantity > 10 mock trả về fail
        var response = await client.PostAsJsonAsync("/api/orders", new { ProductId = 1, Quantity = 15 });
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetOrder_ValidId_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/orders/1");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetOrder_InvalidId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/orders/999");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }
}
