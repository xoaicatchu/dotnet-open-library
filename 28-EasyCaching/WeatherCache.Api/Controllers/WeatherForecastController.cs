using EasyCaching.Core;
using Microsoft.AspNetCore.Mvc;
using WeatherCache.Api.Models;

namespace WeatherCache.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WeatherForecastController : ControllerBase
{
    private readonly IEasyCachingProvider _cache;
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    public WeatherForecastController(IEasyCachingProviderFactory factory)
    {
        _cache = factory.GetCachingProvider("default_mem");
    }

    /// <summary>
    /// Gets weather forecast for a city, using EasyCaching IEasyCachingProvider.
    /// If cached, returns immediately; otherwise generates and caches for 2 minutes.
    /// </summary>
    [HttpGet("{city}")]
    [ProducesResponseType(typeof(CachedForecastResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CachedForecastResponse>> Get(string city)
    {
        if (string.IsNullOrWhiteSpace(city))
        {
            return BadRequest(new { message = "City name is required." });
        }

        var key = $"weather:{city.Trim().ToLowerInvariant()}";
        var cacheValue = await _cache.GetAsync<WeatherForecast>(key);

        if (cacheValue.HasValue)
        {
            return Ok(new CachedForecastResponse(cacheValue.Value, true, _cache.Name));
        }

        // Cache miss: generate fresh forecast
        var rng = new Random(city.GetHashCode());
        var tempC = rng.Next(-10, 40);
        var summary = Summaries[rng.Next(Summaries.Length)];

        var forecast = new WeatherForecast(
            City: char.ToUpper(city[0]) + city[1..].ToLowerInvariant(),
            Date: DateOnly.FromDateTime(DateTime.UtcNow),
            TemperatureC: tempC,
            Summary: summary,
            CachedAt: DateTime.UtcNow
        );

        await _cache.SetAsync(key, forecast, TimeSpan.FromMinutes(2));

        return Ok(new CachedForecastResponse(forecast, false, _cache.Name));
    }

    /// <summary>
    /// Updates or inserts a custom weather forecast into EasyCaching with custom TTL.
    /// </summary>
    [HttpPost("{city}")]
    [ProducesResponseType(typeof(CachedForecastResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CachedForecastResponse>> Set(string city, [FromBody] SetWeatherForecastRequest request)
    {
        if (string.IsNullOrWhiteSpace(city))
        {
            return BadRequest(new { message = "City name is required." });
        }

        if (string.IsNullOrWhiteSpace(request.Summary))
        {
            return BadRequest(new { message = "Summary is required." });
        }

        var ttl = request.TtlSeconds > 0 ? TimeSpan.FromSeconds(request.TtlSeconds) : TimeSpan.FromMinutes(2);
        var key = $"weather:{city.Trim().ToLowerInvariant()}";

        var forecast = new WeatherForecast(
            City: char.ToUpper(city[0]) + city[1..].ToLowerInvariant(),
            Date: DateOnly.FromDateTime(DateTime.UtcNow),
            TemperatureC: request.TemperatureC,
            Summary: request.Summary.Trim(),
            CachedAt: DateTime.UtcNow
        );

        await _cache.SetAsync(key, forecast, ttl);

        return Ok(new CachedForecastResponse(forecast, true, _cache.Name));
    }

    /// <summary>
    /// Evicts a city's weather forecast from EasyCaching.
    /// </summary>
    [HttpDelete("{city}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Evict(string city)
    {
        var key = $"weather:{city.Trim().ToLowerInvariant()}";
        await _cache.RemoveAsync(key);
        return NoContent();
    }

    /// <summary>
    /// Retrieves all cached items matching a key prefix.
    /// </summary>
    [HttpGet("prefix/{prefix}")]
    [ProducesResponseType(typeof(IDictionary<string, CacheValue<WeatherForecast>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByPrefix(string prefix)
    {
        var result = await _cache.GetByPrefixAsync<WeatherForecast>(prefix);
        return Ok(result);
    }
}
