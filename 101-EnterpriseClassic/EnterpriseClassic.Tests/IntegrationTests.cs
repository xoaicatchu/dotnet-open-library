using System.Net;
using System.Net.Http.Json;
using EnterpriseClassic.Api.Data;
using EnterpriseClassic.Api.Features.Orders;
using EnterpriseClassic.Api.Features.Products;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EnterpriseClassic.Tests;

public class IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    
    public IntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(b =>
            b.ConfigureServices(services => {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);
                
                services.AddDbContext<AppDbContext>(opt =>
                    opt.UseSqlite("Data Source=enterprise-test.db"));
            }))
        .CreateClient();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
        AppDbContext.Seed(db);
    }

    [Fact]
    public async Task GetProducts_Returns200()
    {
        var response = await _client.GetAsync("/api/products");
        response.EnsureSuccessStatusCode();
        var products = await response.Content.ReadFromJsonAsync<List<ProductSummaryDto>>();
        Assert.NotNull(products);
    }

    [Fact]
    public async Task GetProductById_Returns200()
    {
        var createReq = new CreateProductRequest("Test", "Desc", 10.5m, 100, 1);
        var createRes = await _client.PostAsJsonAsync("/api/products", createReq);
        var product = await createRes.Content.ReadFromJsonAsync<ProductDto>();
        
        var response = await _client.GetAsync($"/api/products/{product!.Id}");
        response.EnsureSuccessStatusCode();
        var fetched = await response.Content.ReadFromJsonAsync<ProductDto>();
        Assert.Equal(product.Id, fetched!.Id);
    }

    [Fact]
    public async Task GetProductById_Returns404()
    {
        var response = await _client.GetAsync("/api/products/999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_Returns201()
    {
        var req = new CreateProductRequest("NewProd", "Desc", 50, 10, 2);
        var res = await _client.PostAsJsonAsync("/api/products", req);
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var product = await res.Content.ReadFromJsonAsync<ProductDto>();
        Assert.Equal("NewProd", product!.Name);
    }

    [Fact]
    public async Task CreateProduct_InvalidInput_Returns400()
    {
        var req = new CreateProductRequest("", "Desc", -5, -1, 0);
        var res = await _client.PostAsJsonAsync("/api/products", req);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task UpdateProduct_Returns200()
    {
        var createReq = new CreateProductRequest("Test", "Desc", 10.5m, 100, 1);
        var createRes = await _client.PostAsJsonAsync("/api/products", createReq);
        var product = await createRes.Content.ReadFromJsonAsync<ProductDto>();

        var updateReq = new UpdateProductRequest("Updated", "Desc", 20, 50, 1);
        var updateRes = await _client.PutAsJsonAsync($"/api/products/{product!.Id}", updateReq);
        updateRes.EnsureSuccessStatusCode();
        
        var updated = await updateRes.Content.ReadFromJsonAsync<ProductDto>();
        Assert.Equal("Updated", updated!.Name);
    }

    [Fact]
    public async Task GetOrders_Returns200()
    {
        var response = await _client.GetAsync("/api/orders");
        response.EnsureSuccessStatusCode();
        var orders = await response.Content.ReadFromJsonAsync<List<OrderSummaryDto>>();
        Assert.NotNull(orders);
    }

    [Fact]
    public async Task CreateOrder_Returns201()
    {
        var req = new CreateOrderRequest("John Doe", "john@test.com", new List<CreateOrderItemRequest>
        {
            new CreateOrderItemRequest("Product 1", 2, 50)
        });
        
        var res = await _client.PostAsJsonAsync("/api/orders", req);
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var order = await res.Content.ReadFromJsonAsync<OrderDto>();
        Assert.Equal("John Doe", order!.CustomerName);
        Assert.Single(order.Items);
    }
}
