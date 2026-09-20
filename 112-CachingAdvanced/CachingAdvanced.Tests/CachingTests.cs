using System.Net;
using System.Net.Http.Json;
using CachingAdvanced.Api.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CachingAdvanced.Tests;

public class CachingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public CachingTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Test1_IMemoryCache_MissDBHitSet()
    {
        var response = await _client.GetAsync("/api/memorycache/1");
        response.EnsureSuccessStatusCode();
        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        Assert.NotNull(product);
        Assert.Equal(1, product.Id);
    }

    [Fact]
    public async Task Test2_IMemoryCache_Hit()
    {
        await _client.GetAsync("/api/memorycache/1");
        var response = await _client.GetAsync("/api/memorycache/1");
        response.EnsureSuccessStatusCode();
        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        Assert.NotNull(product);
    }

    [Fact]
    public async Task Test3_IMemoryCache_Invalidate()
    {
        await _client.GetAsync("/api/memorycache/1");
        var invResponse = await _client.PostAsync("/api/memorycache/1/invalidate", null);
        invResponse.EnsureSuccessStatusCode();
        
        var response = await _client.GetAsync("/api/memorycache/1");
        response.EnsureSuccessStatusCode();
        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        Assert.NotNull(product);
    }

    [Fact]
    public async Task Test4_IDistributedCache_Miss()
    {
        var response = await _client.GetAsync("/api/distributedcache/2");
        response.EnsureSuccessStatusCode();
        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        Assert.NotNull(product);
        Assert.Equal(2, product.Id);
    }

    [Fact]
    public async Task Test5_IDistributedCache_Hit()
    {
        await _client.GetAsync("/api/distributedcache/2");
        var response = await _client.GetAsync("/api/distributedcache/2");
        response.EnsureSuccessStatusCode();
        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        Assert.NotNull(product);
    }

    [Fact]
    public async Task Test6_HybridCache_Set()
    {
        var response = await _client.GetAsync("/api/hybridcache/3");
        response.EnsureSuccessStatusCode();
        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        Assert.NotNull(product);
        Assert.Equal(3, product.Id);
    }

    [Fact]
    public async Task Test7_HybridCache_Hit()
    {
        await _client.GetAsync("/api/hybridcache/3");
        var response = await _client.GetAsync("/api/hybridcache/3");
        response.EnsureSuccessStatusCode();
        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        Assert.NotNull(product);
    }

    [Fact]
    public async Task Test8_FusionCache_Set()
    {
        var response = await _client.GetAsync("/api/fusioncache/1");
        response.EnsureSuccessStatusCode();
        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        Assert.NotNull(product);
        Assert.Equal(1, product.Id);
    }

    [Fact]
    public async Task Test9_FusionCache_NonExistent()
    {
        var response = await _client.GetAsync("/api/fusioncache/999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Test10_CacheComparison()
    {
        var m1 = await _client.GetFromJsonAsync<ProductDto>("/api/memorycache/2");
        var d1 = await _client.GetFromJsonAsync<ProductDto>("/api/distributedcache/2");
        var h1 = await _client.GetFromJsonAsync<ProductDto>("/api/hybridcache/2");
        var f1 = await _client.GetFromJsonAsync<ProductDto>("/api/fusioncache/2");
        
        Assert.NotNull(m1);
        Assert.Equal(m1.Id, d1?.Id);
        Assert.Equal(m1.Id, h1?.Id);
        Assert.Equal(m1.Id, f1?.Id);
    }
}
