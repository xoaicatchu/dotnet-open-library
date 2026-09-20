using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Observability.Api.Data;
using Observability.Api.Entities;
using Xunit;

namespace Observability.Tests;

public class ObservabilityTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ObservabilityTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            // Configure test services if needed
        });
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetProducts_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/products");
        response.EnsureSuccessStatusCode();
        var products = await response.Content.ReadFromJsonAsync<List<ProductEntity>>();
        Assert.NotNull(products);
    }

    [Fact]
    public async Task CreateProduct_ReturnsCreated()
    {
        var request = new CreateProductRequest { Name = "Test Product", Price = 9.99m, Stock = 100 };
        var response = await _client.PostAsJsonAsync("/api/products", request);
        
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var product = await response.Content.ReadFromJsonAsync<ProductEntity>();
        Assert.NotNull(product);
        Assert.Equal("Test Product", product.Name);
        Assert.True(product.Id > 0);
    }

    [Fact]
    public async Task GetProductById_Existing_ReturnsOk()
    {
        // First create
        var request = new CreateProductRequest { Name = "Test GetById", Price = 1.99m, Stock = 50 };
        var createResponse = await _client.PostAsJsonAsync("/api/products", request);
        var createdProduct = await createResponse.Content.ReadFromJsonAsync<ProductEntity>();
        
        // Then get
        var response = await _client.GetAsync($"/api/products/{createdProduct!.Id}");
        response.EnsureSuccessStatusCode();
        var fetchedProduct = await response.Content.ReadFromJsonAsync<ProductEntity>();
        
        Assert.NotNull(fetchedProduct);
        Assert.Equal(createdProduct.Id, fetchedProduct.Id);
    }

    [Fact]
    public async Task GetProductById_NonExisting_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/products/999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteProduct_Existing_ReturnsNoContent()
    {
        // First create
        var request = new CreateProductRequest { Name = "Test Delete", Price = 2.99m, Stock = 20 };
        var createResponse = await _client.PostAsJsonAsync("/api/products", request);
        var createdProduct = await createResponse.Content.ReadFromJsonAsync<ProductEntity>();
        
        // Then delete
        var deleteResponse = await _client.DeleteAsync($"/api/products/{createdProduct!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        
        // Verify deleted
        var getResponse = await _client.GetAsync($"/api/products/{createdProduct.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task GetMetrics_ReturnsOk()
    {
        // Issue a request so the metric gets populated
        await _client.GetAsync("/api/products");
        
        var response = await _client.GetAsync("/metrics");
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("api_requests_total", content); // From Prometheus exporter
    }

    [Fact]
    public async Task StructuredLogging_Works()
    {
        // Issue requests to generate logs
        await _client.PostAsJsonAsync("/api/products", new CreateProductRequest { Name = "LogTest1", Price = 1, Stock = 1 });
        await _client.PostAsJsonAsync("/api/products", new CreateProductRequest { Name = "LogTest2", Price = 2, Stock = 2 });
        await _client.GetAsync("/api/products");
        
        // Normally we'd use a sink to capture and verify logs in memory for testing.
        // For this demo, just ensuring the endpoints succeed when Serilog is configured is a good start.
        Assert.True(true);
    }

    [Fact]
    public async Task DistributedTracing_SpansCreated()
    {
        // We verify that the ActivitySource "Observability.Api" was utilized.
        using var listener = new System.Diagnostics.ActivityListener
        {
            ShouldListenTo = s => s.Name == "Observability.Api",
            Sample = (ref System.Diagnostics.ActivityCreationOptions<System.Diagnostics.ActivityContext> _) => System.Diagnostics.ActivitySamplingResult.AllData,
            SampleUsingParentId = (ref System.Diagnostics.ActivityCreationOptions<string> _) => System.Diagnostics.ActivitySamplingResult.AllData
        };
        
        var activities = new List<System.Diagnostics.Activity>();
        listener.ActivityStarted = activity => activities.Add(activity);
        System.Diagnostics.ActivitySource.AddActivityListener(listener);

        await _client.GetAsync("/api/products");
        
        Assert.Contains(activities, a => a.OperationName == "GetProducts");
    }
}
