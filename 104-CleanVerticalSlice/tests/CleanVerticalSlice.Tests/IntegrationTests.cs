namespace CleanVerticalSlice.Tests;

using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using CleanVerticalSlice.Application.Products.GetProducts;
using CleanVerticalSlice.Application.Products.GetProductById;
using CleanVerticalSlice.Application.Products.CreateProduct;
using CleanVerticalSlice.Application.Products.UpdateProduct;
using CleanVerticalSlice.Application.Orders.GetOrders;
using CleanVerticalSlice.Application.Orders.CreateOrder;
using CleanVerticalSlice.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using System;

public class IntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly SqliteConnection _connection;

    public IntegrationTests(WebApplicationFactory<Program> factory)
    {
        _connection = new SqliteConnection("DataSource=TestDb;Mode=Memory;Cache=Shared");
        _connection.Open();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);
                
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseSqlite(_connection);
                });

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
                DbSeeder.Seed(db);
            });
        });
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }

    [Fact]
    public async Task GetProducts_Returns200AndData()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/products");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var products = await response.Content.ReadFromJsonAsync<System.Collections.Generic.List<ProductDto>>();
        products.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetProductById_Exists_Returns200()
    {
        var client = _factory.CreateClient();
        var productsResponse = await client.GetFromJsonAsync<System.Collections.Generic.List<ProductDto>>("/api/products");
        var id = productsResponse![0].Id;
        
        var response = await client.GetAsync($"/api/products/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var product = await response.Content.ReadFromJsonAsync<ProductDetailDto>();
        product!.Id.Should().Be(id);
    }

    [Fact]
    public async Task GetProductById_NotExists_Returns404()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/products/99999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateProduct_Valid_Returns201()
    {
        var client = _factory.CreateClient();
        var request = new CreateProductRequest { Name = "Test", Description = "Desc", Price = 100, Stock = 10, CategoryId = 1 };
        var response = await client.PostAsJsonAsync("/api/products", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateProduct_InvalidEmptyName_Returns400()
    {
        var client = _factory.CreateClient();
        var request = new CreateProductRequest { Name = "", Description = "Desc", Price = 100, Stock = 10, CategoryId = 1 };
        var response = await client.PostAsJsonAsync("/api/products", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateProduct_InvalidNegativePrice_Returns400()
    {
        var client = _factory.CreateClient();
        var request = new CreateProductRequest { Name = "Test", Description = "Desc", Price = -10, Stock = 10, CategoryId = 1 };
        var response = await client.PostAsJsonAsync("/api/products", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateProduct_Exists_Returns200()
    {
        var client = _factory.CreateClient();
        var productsResponse = await client.GetFromJsonAsync<System.Collections.Generic.List<ProductDto>>("/api/products");
        var id = productsResponse![0].Id;

        var request = new UpdateProductRequest { Name = "Updated", Description = "Desc", Price = 200, Stock = 20 };
        var response = await client.PutAsJsonAsync($"/api/products/{id}", request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateProduct_NotExists_Returns404()
    {
        var client = _factory.CreateClient();
        var request = new UpdateProductRequest { Name = "Updated", Description = "Desc", Price = 200, Stock = 20 };
        var response = await client.PutAsJsonAsync("/api/products/99999", request);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetOrders_Returns200()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/orders");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateOrder_Valid_Returns201()
    {
        var client = _factory.CreateClient();
        var request = new CreateOrderRequest 
        { 
            CustomerEmail = "test@test.com", 
            Items = new System.Collections.Generic.List<OrderItemRequest> { new OrderItemRequest { ProductId = 1, Quantity = 2, UnitPrice = 100 } } 
        };
        var response = await client.PostAsJsonAsync("/api/orders", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created); // Note: using Created instead of CreatedAtAction
    }
}
