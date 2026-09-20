using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TestingMastery.Api.Data;
using TestingMastery.Api.Entities;
using TestingMastery.Api.Services;

namespace TestingMastery.Tests.Integration;

public class ProductIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ProductIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
                
                var connectionString = $"DataSource=file:{Guid.NewGuid()}?mode=memory&cache=shared";
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseSqlite(connectionString);
                });
                
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();
                db.Products.Add(new ProductEntity { Name = "Seeded", Price = 10, Stock = 5 });
                db.SaveChanges();
            });
        });
    }

    [Fact]
    public async Task GetAll_ReturnsSuccessAndList()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/products");
        response.EnsureSuccessStatusCode();
        
        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>();
        products.Should().NotBeNull();
        products!.Should().Contain(p => p.Name == "Seeded");
    }

    [Fact]
    public async Task GetById_WhenExists_ReturnsSuccess()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/products/1");
        response.EnsureSuccessStatusCode();
        
        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        product.Should().NotBeNull();
        product!.Id.Should().Be(1);
    }

    [Fact]
    public async Task Create_ReturnsCreated()
    {
        var client = _factory.CreateClient();
        var req = new CreateProductRequest("Integration", 99.99m, 10);
        var response = await client.PostAsJsonAsync("/api/products", req);
        
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        product!.Name.Should().Be("Integration");
    }

    [Fact]
    public async Task Delete_ReturnsNoContent()
    {
        var client = _factory.CreateClient();
        // First create one to delete
        var req = new CreateProductRequest("ToDelete", 99.99m, 10);
        var createRes = await client.PostAsJsonAsync("/api/products", req);
        var created = await createRes.Content.ReadFromJsonAsync<ProductDto>();
        
        var response = await client.DeleteAsync($"/api/products/{created!.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
