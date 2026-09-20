using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using InventoryService.Api.Models;
using InventoryService.Api.Controllers;
using Microsoft.AspNetCore.Hosting;

namespace InventoryService.Tests;

public class InventoryTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public InventoryTests(WebApplicationFactory<Program> factory)
    {
        // WebApplicationFactory default environment is Production, let's force Development for Swagger
        _client = factory.WithWebHostBuilder(builder => 
        {
            builder.UseEnvironment("Development");
        }).CreateClient();
    }

    [Fact]
    public async Task GetItems_ReturnsSeedData()
    {
        var response = await _client.GetAsync("/api/inventory");
        response.EnsureSuccessStatusCode();
        
        var items = await response.Content.ReadFromJsonAsync<List<InventoryItem>>();
        Assert.NotNull(items);
        Assert.True(items.Count >= 2);
    }

    [Fact]
    public async Task CreateItem_ValidData_ReturnsCreated()
    {
        var request = new CreateInventoryRequest("HD-001", "Hard Drive", 50);
        var response = await _client.PostAsJsonAsync("/api/inventory", request);
        
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var item = await response.Content.ReadFromJsonAsync<InventoryItem>();
        Assert.NotNull(item);
        Assert.Equal("HD-001", item.Sku);
        
        // Verify it exists
        var getResponse = await _client.GetAsync($"/api/inventory/{item.Id}");
        getResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CreateItem_EmptySku_ReturnsBadRequest()
    {
        var request = new CreateInventoryRequest("", "Hard Drive", 50);
        var response = await _client.PostAsJsonAsync("/api/inventory", request);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetItem_NonExistent_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/inventory/999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AddStock_IncrementsQuantityAfterDelay()
    {
        // Add a new item for this test
        var req = new CreateInventoryRequest("TST-ADD", "Test Add", 10);
        var createRes = await _client.PostAsJsonAsync("/api/inventory", req);
        var item = await createRes.Content.ReadFromJsonAsync<InventoryItem>();
        
        // Send add stock command
        var addRes = await _client.PostAsJsonAsync($"/api/inventory/{item!.Id}/add-stock", new StockRequest(5));
        Assert.Equal(HttpStatusCode.Accepted, addRes.StatusCode);
        
        // Wait for handler to process
        await Task.Delay(500);
        
        // Check new quantity
        var getRes = await _client.GetAsync($"/api/inventory/{item.Id}");
        var updatedItem = await getRes.Content.ReadFromJsonAsync<InventoryItem>();
        Assert.Equal(15, updatedItem!.Quantity);
    }

    [Fact]
    public async Task RemoveStock_DecrementsQuantityAfterDelay()
    {
        var req = new CreateInventoryRequest("TST-REM", "Test Remove", 20);
        var createRes = await _client.PostAsJsonAsync("/api/inventory", req);
        var item = await createRes.Content.ReadFromJsonAsync<InventoryItem>();
        
        var remRes = await _client.PostAsJsonAsync($"/api/inventory/{item!.Id}/remove-stock", new StockRequest(5));
        Assert.Equal(HttpStatusCode.Accepted, remRes.StatusCode);
        
        await Task.Delay(500);
        
        var getRes = await _client.GetAsync($"/api/inventory/{item.Id}");
        var updatedItem = await getRes.Content.ReadFromJsonAsync<InventoryItem>();
        Assert.Equal(15, updatedItem!.Quantity);
    }

    [Fact]
    public async Task RemoveStock_TooMuch_DoesNotGoBelowZero()
    {
        var req = new CreateInventoryRequest("TST-REM-NEG", "Test Remove Negative", 10);
        var createRes = await _client.PostAsJsonAsync("/api/inventory", req);
        var item = await createRes.Content.ReadFromJsonAsync<InventoryItem>();
        
        var remRes = await _client.PostAsJsonAsync($"/api/inventory/{item!.Id}/remove-stock", new StockRequest(15));
        Assert.Equal(HttpStatusCode.Accepted, remRes.StatusCode);
        
        await Task.Delay(500);
        
        var getRes = await _client.GetAsync($"/api/inventory/{item.Id}");
        var updatedItem = await getRes.Content.ReadFromJsonAsync<InventoryItem>();
        Assert.Equal(10, updatedItem!.Quantity);
    }
    
    [Fact]
    public async Task Swagger_IsAvailable()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();
    }
}
