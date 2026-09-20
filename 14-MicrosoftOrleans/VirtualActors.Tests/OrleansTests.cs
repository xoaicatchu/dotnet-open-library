using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using VirtualActors.Api.Models;
using Xunit;

namespace VirtualActors.Tests;

public class OrleansTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public OrleansTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AddItemToCart_ReturnsOk_WithUpdatedSummary()
    {
        var cartId = Guid.NewGuid().ToString();
        var item = new CartItem("SKU-1", "Item 1", 2, 100);

        var response = await _client.PostAsJsonAsync($"/api/carts/{cartId}/items", item);
        response.EnsureSuccessStatusCode();

        var summary = await response.Content.ReadFromJsonAsync<CartSummary>();
        Assert.NotNull(summary);
        Assert.Equal(cartId, summary.CartId);
        Assert.Single(summary.Items);
        Assert.Equal(200, summary.TotalAmount);
    }

    [Fact]
    public async Task MultipleItems_CalculateTotalAmountAccurately()
    {
        var cartId = Guid.NewGuid().ToString();
        await _client.PostAsJsonAsync($"/api/carts/{cartId}/items", new CartItem("SKU-1", "Item 1", 2, 100));
        await _client.PostAsJsonAsync($"/api/carts/{cartId}/items", new CartItem("SKU-2", "Item 2", 1, 50));

        var response = await _client.GetAsync($"/api/carts/{cartId}");
        response.EnsureSuccessStatusCode();

        var summary = await response.Content.ReadFromJsonAsync<CartSummary>();
        Assert.NotNull(summary);
        Assert.Equal(2, summary.Items.Count);
        Assert.Equal(250, summary.TotalAmount);
    }

    [Fact]
    public async Task RemoveItem_UpdatesCartCorrectly()
    {
        var cartId = Guid.NewGuid().ToString();
        await _client.PostAsJsonAsync($"/api/carts/{cartId}/items", new CartItem("SKU-1", "Item 1", 2, 100));
        await _client.PostAsJsonAsync($"/api/carts/{cartId}/items", new CartItem("SKU-2", "Item 2", 1, 50));

        var deleteResponse = await _client.DeleteAsync($"/api/carts/{cartId}/items/SKU-1");
        deleteResponse.EnsureSuccessStatusCode();

        var getResponse = await _client.GetAsync($"/api/carts/{cartId}");
        var summary = await getResponse.Content.ReadFromJsonAsync<CartSummary>();
        
        Assert.NotNull(summary);
        Assert.Single(summary.Items);
        Assert.Equal("SKU-2", summary.Items[0].Sku);
        Assert.Equal(50, summary.TotalAmount);
    }

    [Fact]
    public async Task ClearCart_EmptiesTheCart()
    {
        var cartId = Guid.NewGuid().ToString();
        await _client.PostAsJsonAsync($"/api/carts/{cartId}/items", new CartItem("SKU-1", "Item 1", 2, 100));

        var deleteResponse = await _client.DeleteAsync($"/api/carts/{cartId}");
        deleteResponse.EnsureSuccessStatusCode();

        var getResponse = await _client.GetAsync($"/api/carts/{cartId}");
        var summary = await getResponse.Content.ReadFromJsonAsync<CartSummary>();
        
        Assert.NotNull(summary);
        Assert.Empty(summary.Items);
        Assert.Equal(0, summary.TotalAmount);
    }

    [Fact]
    public async Task Counter_IncrementsAtomically()
    {
        var counterId = Guid.NewGuid().ToString();
        
        var inc1 = await _client.PostAsync($"/api/counters/{counterId}/increment?val=5", null);
        inc1.EnsureSuccessStatusCode();
        var inc2 = await _client.PostAsync($"/api/counters/{counterId}/increment?val=3", null);
        inc2.EnsureSuccessStatusCode();

        var getResponse = await _client.GetAsync($"/api/counters/{counterId}");
        getResponse.EnsureSuccessStatusCode();
        
        var count = await getResponse.Content.ReadFromJsonAsync<int>();
        Assert.Equal(8, count);
    }

    [Fact]
    public async Task SwaggerUI_Returns200Ok_InDevelopment()
    {
        var client = _factory.WithWebHostBuilder(builder => 
        {
            builder.UseEnvironment("Development");
        }).CreateClient();

        var response = await client.GetAsync("/swagger/index.html");
        
        response.EnsureSuccessStatusCode();
    }
}
