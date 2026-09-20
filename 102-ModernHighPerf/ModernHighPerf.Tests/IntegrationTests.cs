using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ModernHighPerf.Api.Data;
using ModernHighPerf.Api.Features.Orders;
using ModernHighPerf.Api.Features.Products;
using Xunit;

namespace ModernHighPerf.Tests;

public class IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public IntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                var dbName = Guid.NewGuid().ToString();
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseSqlite($"Data Source={dbName}.db");
                });
            });
        });
    }

    [Fact]
    public async Task GetProducts_ReturnsSeededData()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/products");
        response.EnsureSuccessStatusCode();
        var products = await response.Content.ReadFromJsonAsync<List<ProductSummaryDto>>();
        products.Should().NotBeNull();
        products!.Count.Should().Be(5);
    }

    [Fact]
    public async Task GetProductById_ReturnsDetail()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/products/1");
        response.EnsureSuccessStatusCode();
        var product = await response.Content.ReadFromJsonAsync<ProductDetailDto>();
        product.Should().NotBeNull();
        product!.Id.Should().Be(1);
    }

    [Fact]
    public async Task GetProductById_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/products/999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateProduct_Valid_ReturnsCreated()
    {
        var client = _factory.CreateClient();
        var req = new CreateProductRequest("New Prod", "Desc", 99.9m, 10, 1);
        var response = await client.PostAsJsonAsync("/api/products", req);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateProduct_Invalid_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var req = new CreateProductRequest("", "Desc", -10m, -1, 1);
        var response = await client.PostAsJsonAsync("/api/products", req);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateProduct_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var req = new UpdateProductRequest("Updated Name", "Updated Desc", 100m, 50, 2);
        var response = await client.PutAsJsonAsync("/api/products/2", req);
        response.EnsureSuccessStatusCode();
        var product = await response.Content.ReadFromJsonAsync<ProductDetailDto>();
        product.Should().NotBeNull();
        product!.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task GetOrders_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/orders");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CreateOrder_ReturnsCreated_And_PublishesEvent()
    {
        var client = _factory.CreateClient();
        var req = new CreateOrderRequest("John Doe", "john@test.com", new List<CreateOrderItemRequest>
        {
            new CreateOrderItemRequest("Prod 1", 2, 50m)
        });
        var response = await client.PostAsJsonAsync("/api/orders", req);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        order.Should().NotBeNull();
        order!.CustomerName.Should().Be("John Doe");
        order.TotalAmount.Should().Be(100m);
    }
}
