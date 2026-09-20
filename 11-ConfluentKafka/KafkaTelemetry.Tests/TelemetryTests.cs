using System.Net;
using System.Net.Http.Json;
using KafkaTelemetry.Api.Data;
using KafkaTelemetry.Api.Controllers;
using KafkaTelemetry.Api.Kafka;
using KafkaTelemetry.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace KafkaTelemetry.Tests;

public class FakeTelemetryProducer : ITelemetryProducer
{
    public TelemetryRecord? LastRecord { get; private set; }

    public Task ProduceAsync(TelemetryRecord record, CancellationToken ct = default)
    {
        LastRecord = record;
        return Task.CompletedTask;
    }
}

public class TelemetryTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public TelemetryTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the actual producer and background service
                var producerDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ITelemetryProducer));
                if (producerDescriptor != null) services.Remove(producerDescriptor);
                
                var hostedService = services.SingleOrDefault(d => d.ImplementationType == typeof(TelemetryConsumerService));
                if (hostedService != null) services.Remove(hostedService);

                services.AddSingleton<ITelemetryProducer, FakeTelemetryProducer>();
            });
        });
    }

    [Fact]
    public async Task IngestTelemetry_ReturnsAccepted_AndLocation()
    {
        var client = _factory.CreateClient();
        var request = new TelemetryRequest("sensor-test", 25.0, 60.0);
        
        var response = await client.PostAsJsonAsync("/api/telemetry", request);
        
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal("/api/telemetry/sensor-test", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task DirectStoreSimulation_ReturnsData()
    {
        // Simulate reading from store after consumer saves it
        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<TelemetryStore>();
        
        store.Save(new TelemetryRecord("sensor-store", 22.2, 50.0, DateTime.UtcNow));
        
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/telemetry/sensor-store");
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var record = await response.Content.ReadFromJsonAsync<TelemetryRecord>();
        Assert.NotNull(record);
        Assert.Equal("sensor-store", record.DeviceId);
    }

    [Fact]
    public async Task ListDevices_ReturnsAllRecordedDevices()
    {
        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<TelemetryStore>();
        store.Save(new TelemetryRecord("sensor-list-1", 20, 50, DateTime.UtcNow));
        store.Save(new TelemetryRecord("sensor-list-2", 20, 50, DateTime.UtcNow));

        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/telemetry/devices");
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var devices = await response.Content.ReadFromJsonAsync<string[]>();
        Assert.NotNull(devices);
        Assert.Contains("sensor-list-1", devices);
        Assert.Contains("sensor-list-2", devices);
    }

    [Fact]
    public async Task Validation_MissingDeviceId_Returns400()
    {
        var client = _factory.CreateClient();
        var request = new TelemetryRequest("", 25.0, 60.0);
        
        var response = await client.PostAsJsonAsync("/api/telemetry", request);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Validation_InvalidTemperature_Returns400()
    {
        var client = _factory.CreateClient();
        var request = new TelemetryRequest("sensor", 150.0, 60.0);
        
        var response = await client.PostAsJsonAsync("/api/telemetry", request);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Validation_InvalidHumidity_Returns400()
    {
        var client = _factory.CreateClient();
        var request = new TelemetryRequest("sensor", 25.0, 150.0);
        
        var response = await client.PostAsJsonAsync("/api/telemetry", request);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task NonExistentDevice_Returns404()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/telemetry/non-existent");
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SwaggerUI_Endpoint_IsAccessible()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/swagger/index.html");
        
        Assert.True(response.IsSuccessStatusCode);
    }
}
