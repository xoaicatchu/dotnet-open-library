using System.Net;
using System.Net.Http.Json;
using EasyCaching.Core;
using Microsoft.AspNetCore.Mvc.Testing;
using WeatherCache.Api.Models;

namespace WeatherCache.Tests;

public class WeatherCacheTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public WeatherCacheTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_MissThenHit_ReturnsCachedItem()
    {
        var city = $"London-{Guid.NewGuid():N}";

        // 1. First Call: Cache MISS
        var firstRes = await _client.GetAsync($"/api/weatherforecast/{city}");
        Assert.Equal(HttpStatusCode.OK, firstRes.StatusCode);
        var firstData = await firstRes.Content.ReadFromJsonAsync<CachedForecastResponse>();
        Assert.NotNull(firstData);
        Assert.False(firstData.IsFromCache);
        Assert.Equal(city, firstData.Forecast.City, ignoreCase: true);

        // 2. Second Call: Cache HIT
        var secondRes = await _client.GetAsync($"/api/weatherforecast/{city}");
        Assert.Equal(HttpStatusCode.OK, secondRes.StatusCode);
        var secondData = await secondRes.Content.ReadFromJsonAsync<CachedForecastResponse>();
        Assert.NotNull(secondData);
        Assert.True(secondData.IsFromCache);
        Assert.Equal(firstData.Forecast.TemperatureC, secondData.Forecast.TemperatureC);
        Assert.Equal(firstData.Forecast.Summary, secondData.Forecast.Summary);
    }

    [Fact]
    public async Task Set_CustomForecast_CachesWithCustomValues()
    {
        var city = $"Paris-{Guid.NewGuid():N}";
        var request = new SetWeatherForecastRequest(28, "Sunny and Clear", 300);

        var postRes = await _client.PostAsJsonAsync($"/api/weatherforecast/{city}", request);
        Assert.Equal(HttpStatusCode.OK, postRes.StatusCode);
        var postData = await postRes.Content.ReadFromJsonAsync<CachedForecastResponse>();
        Assert.NotNull(postData);
        Assert.Equal(28, postData.Forecast.TemperatureC);
        Assert.Equal("Sunny and Clear", postData.Forecast.Summary);

        // Fetch to verify cached
        var getRes = await _client.GetAsync($"/api/weatherforecast/{city}");
        Assert.Equal(HttpStatusCode.OK, getRes.StatusCode);
        var getData = await getRes.Content.ReadFromJsonAsync<CachedForecastResponse>();
        Assert.NotNull(getData);
        Assert.True(getData.IsFromCache);
        Assert.Equal(28, getData.Forecast.TemperatureC);
    }

    [Fact]
    public async Task Evict_RemovesFromCache_NextGetGeneratesNew()
    {
        var city = $"Berlin-{Guid.NewGuid():N}";

        // Populate cache
        await _client.GetAsync($"/api/weatherforecast/{city}");

        // Evict
        var deleteRes = await _client.DeleteAsync($"/api/weatherforecast/{city}");
        Assert.Equal(HttpStatusCode.NoContent, deleteRes.StatusCode);

        // Next GET should be cache miss
        var getRes = await _client.GetAsync($"/api/weatherforecast/{city}");
        Assert.Equal(HttpStatusCode.OK, getRes.StatusCode);
        var getData = await getRes.Content.ReadFromJsonAsync<CachedForecastResponse>();
        Assert.NotNull(getData);
        Assert.False(getData.IsFromCache);
    }

    [Fact]
    public async Task GetByPrefix_ReturnsMatchingKeys()
    {
        var prefix = $"pref-{Guid.NewGuid():N}";
        var city1 = $"{prefix}-City1";
        var city2 = $"{prefix}-City2";

        // Seed 2 cities
        await _client.GetAsync($"/api/weatherforecast/{city1}");
        await _client.GetAsync($"/api/weatherforecast/{city2}");

        // Query by prefix
        var prefixRes = await _client.GetAsync($"/api/weatherforecast/prefix/weather:{prefix}");
        Assert.Equal(HttpStatusCode.OK, prefixRes.StatusCode);

        var result = await prefixRes.Content.ReadFromJsonAsync<Dictionary<string, CacheValue<WeatherForecast>>>();
        Assert.NotNull(result);
        Assert.True(result.Count >= 2);
    }

    [Fact]
    public async Task Get_WithWhitespaceCity_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/api/weatherforecast/%20");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
