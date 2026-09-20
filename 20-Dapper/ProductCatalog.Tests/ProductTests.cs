using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ProductCatalog.Api.Data;
using ProductCatalog.Api.Models;

namespace ProductCatalog.Tests;

public class ProductTestsFixture : WebApplicationFactory<Program>
{
    private readonly string _dbPath;

    public ProductTestsFixture()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"products_test_{Guid.NewGuid():N}.db");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IDbConnectionFactory));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            var connStr = $"Data Source={_dbPath}";
            var factory = new SqliteConnectionFactory(connStr);
            services.AddSingleton<IDbConnectionFactory>(factory);

            DbInitializer.Initialize(factory);
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

public class ProductTests : IClassFixture<ProductTestsFixture>
{
    private readonly HttpClient _client;

    public ProductTests(ProductTestsFixture fixture)
    {
        _client = fixture.CreateClient();
    }

    [Fact]
    public async Task GetAll_ReturnsSeededProductsWithCategoryNames()
    {
        var response = await _client.GetAsync("/api/products");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var products = await response.Content.ReadFromJsonAsync<List<ProductResponseDto>>();
        Assert.NotNull(products);
        Assert.NotEmpty(products);
        Assert.All(products, p => Assert.NotNull(p.CategoryName));
    }

    [Fact]
    public async Task GetAll_WithSearch_FiltersResults()
    {
        var response = await _client.GetAsync("/api/products?search=Keyboard");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var products = await response.Content.ReadFromJsonAsync<List<ProductResponseDto>>();
        Assert.NotNull(products);
        Assert.Single(products);
        Assert.Contains("Keyboard", products[0].Name);
    }

    [Fact]
    public async Task GetAll_WithCategoryId_FiltersResults()
    {
        var response = await _client.GetAsync("/api/products?categoryId=2");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var products = await response.Content.ReadFromJsonAsync<List<ProductResponseDto>>();
        Assert.NotNull(products);
        Assert.All(products, p => Assert.Equal(2, p.CategoryId));
    }

    [Fact]
    public async Task GetById_WithValidId_ReturnsProductAndCategory()
    {
        var response = await _client.GetAsync("/api/products/1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var product = await response.Content.ReadFromJsonAsync<ProductResponseDto>();
        Assert.NotNull(product);
        Assert.Equal(1, product.Id);
        Assert.Equal("Electronics", product.CategoryName);
    }

    [Fact]
    public async Task GetById_WithNonExistentId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/products/9999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithValidData_ReturnsCreatedAndLocationHeader()
    {
        var createDto = new CreateProductDto(
            Name: "USB-C Hub Multiport Adapter",
            Description: "7-in-1 USB-C Hub with HDMI and Power Delivery",
            Price: 49.99m,
            Stock: 120,
            CategoryId: 1
        );

        var response = await _client.PostAsJsonAsync("/api/products", createDto);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var created = await response.Content.ReadFromJsonAsync<ProductResponseDto>();
        Assert.NotNull(created);
        Assert.True(created.Id > 0);
        Assert.Equal("USB-C Hub Multiport Adapter", created.Name);
        Assert.Equal("Electronics", created.CategoryName);
    }

    [Fact]
    public async Task Create_WithEmptyName_ReturnsBadRequest()
    {
        var createDto = new CreateProductDto(
            Name: "",
            Description: "Invalid product",
            Price: 10.0m,
            Stock: 5,
            CategoryId: 1
        );

        var response = await _client.PostAsJsonAsync("/api/products", createDto);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithNonExistentCategoryId_ReturnsBadRequest()
    {
        var createDto = new CreateProductDto(
            Name: "Orphan Product",
            Description: "Product with invalid category",
            Price: 20.0m,
            Stock: 5,
            CategoryId: 999
        );

        var response = await _client.PostAsJsonAsync("/api/products", createDto);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithValidData_ReturnsUpdatedProduct()
    {
        // First create a product to update
        var createDto = new CreateProductDto("Test Update Prod", "Desc", 50.0m, 10, 1);
        var createRes = await _client.PostAsJsonAsync("/api/products", createDto);
        var created = await createRes.Content.ReadFromJsonAsync<ProductResponseDto>();
        Assert.NotNull(created);

        var updateDto = new UpdateProductDto("Test Update Prod - Modified", "Updated Desc", 55.0m, 12, 1);
        var updateRes = await _client.PutAsJsonAsync($"/api/products/{created.Id}", updateDto);
        Assert.Equal(HttpStatusCode.OK, updateRes.StatusCode);

        var updated = await updateRes.Content.ReadFromJsonAsync<ProductResponseDto>();
        Assert.NotNull(updated);
        Assert.Equal("Test Update Prod - Modified", updated.Name);
        Assert.Equal(55.0m, updated.Price);
    }

    [Fact]
    public async Task Update_WithNonExistentId_ReturnsNotFound()
    {
        var updateDto = new UpdateProductDto("Ghost", "Desc", 10m, 1, 1);
        var response = await _client.PutAsJsonAsync("/api/products/8888", updateDto);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WithValidId_ReturnsNoContent()
    {
        // First create a product to delete
        var createDto = new CreateProductDto("To Be Deleted", "Desc", 15.0m, 1, 1);
        var createRes = await _client.PostAsJsonAsync("/api/products", createDto);
        var created = await createRes.Content.ReadFromJsonAsync<ProductResponseDto>();
        Assert.NotNull(created);

        var deleteRes = await _client.DeleteAsync($"/api/products/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteRes.StatusCode);

        var getRes = await _client.GetAsync($"/api/products/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getRes.StatusCode);
    }

    [Fact]
    public async Task BatchDiscount_AppliesDiscountWithinTransaction()
    {
        var request = new BatchDiscountRequest(
            DiscountPercentage: 10m,
            ProductIds: new List<int> { 1, 2 }
        );

        var response = await _client.PostAsJsonAsync("/api/products/batch-discount", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<BatchDiscountResult>();
        Assert.NotNull(result);
        Assert.Equal(2, result.AffectedCount);
    }

    [Fact]
    public async Task BatchDiscount_WithInvalidPercentage_ReturnsBadRequest()
    {
        var request = new BatchDiscountRequest(
            DiscountPercentage: 150m, // Invalid > 100
            ProductIds: new List<int> { 1 }
        );

        var response = await _client.PostAsJsonAsync("/api/products/batch-discount", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
