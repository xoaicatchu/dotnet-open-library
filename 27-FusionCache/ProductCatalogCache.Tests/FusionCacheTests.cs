using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using ProductCatalogCache.Api.Models;

namespace ProductCatalogCache.Tests;

public class FusionCacheTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public FusionCacheTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CacheLifecycle_Miss_Hit_Stampede_FailSafe_Eviction()
    {
        // Reset stats
        await _client.PostAsync("/api/productscache/reset", null);

        var productId = 42;

        // 1. First Call: Cache MISS (hits simulated DB)
        var firstRes = await _client.GetAsync($"/api/productscache/{productId}");
        Assert.Equal(HttpStatusCode.OK, firstRes.StatusCode);
        var firstData = await firstRes.Content.ReadFromJsonAsync<CachedProductResponse>();
        Assert.NotNull(firstData);
        Assert.Equal(1, firstData.TotalDbQueries);
        Assert.Equal(productId, firstData.Product.Id);

        // 2. Second Call: Cache HIT (returns from L1 MemoryCache, DB query count remains 1)
        var secondRes = await _client.GetAsync($"/api/productscache/{productId}");
        Assert.Equal(HttpStatusCode.OK, secondRes.StatusCode);
        var secondData = await secondRes.Content.ReadFromJsonAsync<CachedProductResponse>();
        Assert.NotNull(secondData);
        Assert.Equal(1, secondData.TotalDbQueries); // DB count did not increase!

        // 3. Stampede Protection: Fire 10 concurrent requests to a new ID
        var stampedeId = 99;
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => _client.GetAsync($"/api/productscache/{stampedeId}"))
            .ToArray();

        var responses = await Task.WhenAll(tasks);
        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));

        // In stats, stampedeId should only have triggered exactly 1 DB query!
        // Total DB queries so far: 1 (from productId 42) + 1 (from stampedeId 99) = 2
        var statsRes = await _client.GetAsync("/api/productscache/stats");
        var stats = await statsRes.Content.ReadFromJsonAsync<CacheStatsDto>();
        Assert.NotNull(stats);
        Assert.Equal(2, stats.TotalDbQueries);

        // 4. Fail-Safe: Simulate database failure
        await _client.PostAsync("/api/productscache/simulate-failure?enabled=true", null);

        // Request productId 42 while DB is down: FusionCache Fail-Safe safely returns cached item!
        var failSafeRes = await _client.GetAsync($"/api/productscache/{productId}");
        Assert.Equal(HttpStatusCode.OK, failSafeRes.StatusCode);
        var failSafeData = await failSafeRes.Content.ReadFromJsonAsync<CachedProductResponse>();
        Assert.NotNull(failSafeData);
        Assert.Equal(productId, failSafeData.Product.Id);

        // Restore DB
        await _client.PostAsync("/api/productscache/simulate-failure?enabled=false", null);

        // 5. Eviction: Delete from cache
        var deleteRes = await _client.DeleteAsync($"/api/productscache/{productId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteRes.StatusCode);

        // Next request after eviction hits DB again (increments total from 2 to 3)
        var postEvictRes = await _client.GetAsync($"/api/productscache/{productId}");
        Assert.Equal(HttpStatusCode.OK, postEvictRes.StatusCode);
        var postEvictData = await postEvictRes.Content.ReadFromJsonAsync<CachedProductResponse>();
        Assert.NotNull(postEvictData);
        Assert.Equal(3, postEvictData.TotalDbQueries);
    }

    [Fact]
    public async Task Get_WithInvalidId_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/api/productscache/-1");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
