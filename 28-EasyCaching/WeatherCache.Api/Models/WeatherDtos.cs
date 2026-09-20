namespace WeatherCache.Api.Models;

public record WeatherForecast(
    string City,
    DateOnly Date,
    int TemperatureC,
    string Summary,
    DateTime CachedAt
)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public record SetWeatherForecastRequest(
    int TemperatureC,
    string Summary,
    int TtlSeconds = 120
);

public record CachedForecastResponse(
    WeatherForecast Forecast,
    bool IsFromCache,
    string CacheProvider
);
