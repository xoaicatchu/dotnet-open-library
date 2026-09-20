using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SwaggerDocumentation.Api.Models;
using Xunit;

namespace SwaggerDocumentation.Tests;

public class SwaggerDocTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SwaggerDocTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetSwaggerV1Json_Returns200WithOpenApiDoc()
    {
        // Act
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("Products API - V1", json);
        Assert.Contains("/api/v1/products", json);
        Assert.Contains("X-Correlation-Id", json);
        Assert.Contains("Bearer", json);
    }

    [Fact]
    public async Task GetSwaggerV2Json_Returns200WithOpenApiDoc()
    {
        // Act
        var response = await _client.GetAsync("/swagger/v2/swagger.json");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("Products API - V2 (Enhanced)", json);
        Assert.Contains("/api/v2/products", json);
        Assert.Contains("X-Correlation-Id", json);
    }

    [Fact]
    public async Task GetSwaggerUI_Returns200()
    {
        // Act
        var response = await _client.GetAsync("/swagger/index.html");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Swagger UI", html);
    }

    [Fact]
    public async Task GetProductsV1_Returns200()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/products");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var products = await response.Content.ReadFromJsonAsync<List<ProductV1>>();
        Assert.NotNull(products);
        Assert.NotEmpty(products);
    }

    [Fact]
    public async Task GetProductsV2_Returns200()
    {
        // Act
        var response = await _client.GetAsync("/api/v2/products");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var products = await response.Content.ReadFromJsonAsync<List<ProductV2>>();
        Assert.NotNull(products);
        Assert.NotEmpty(products);
        Assert.Contains(products, p => !string.IsNullOrEmpty(p.Sku));
    }

    [Fact]
    public async Task CreateProductV1_Valid_Returns201WithLocation()
    {
        // Arrange
        var request = new CreateProductV1Request("Wireless Mouse", 29.99m, "Peripherals");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/products", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var created = await response.Content.ReadFromJsonAsync<ProductV1>();
        Assert.NotNull(created);
        Assert.Equal("Wireless Mouse", created.Name);
    }

    [Fact]
    public async Task CreateProductV2_Valid_Returns201WithLocation()
    {
        // Arrange
        var request = new CreateProductV2Request("KEY-MECH-01", "Mechanical Keyboard", 129.99m, "Keyboards", true, ["rgb", "usb-c"]);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v2/products", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var created = await response.Content.ReadFromJsonAsync<ProductV2>();
        Assert.NotNull(created);
        Assert.Equal("KEY-MECH-01", created.Sku);
        Assert.Equal(5.0, created.Rating);
    }

    [Fact]
    public async Task CreateProductV1_EmptyName_Returns400()
    {
        // Arrange
        var request = new CreateProductV1Request("", 10.0m, "Category");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/products", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetProductV1ById_NotFound_Returns404()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/products/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
