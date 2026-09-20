using KafkaTelemetry.Api.Data;
using KafkaTelemetry.Api.Kafka;
using KafkaTelemetry.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace KafkaTelemetry.Api.Controllers;

public record TelemetryRequest(string DeviceId, double Temperature, double Humidity);

[ApiController]
[Route("api/[controller]")]
public class TelemetryController : ControllerBase
{
    private readonly TelemetryStore _store;
    private readonly ITelemetryProducer _producer;

    public TelemetryController(TelemetryStore store, ITelemetryProducer producer)
    {
        _store = store;
        _producer = producer;
    }

    [HttpPost]
    public async Task<IActionResult> Ingest([FromBody] TelemetryRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.DeviceId))
            return BadRequest(new { error = "DeviceId is required" });
        if (req.Temperature < -100 || req.Temperature > 100)
            return BadRequest(new { error = "Temperature out of range (-100 to 100)" });
        if (req.Humidity < 0 || req.Humidity > 100)
            return BadRequest(new { error = "Humidity must be between 0 and 100" });

        var record = new TelemetryRecord(req.DeviceId, req.Temperature, req.Humidity, DateTime.UtcNow);
        await _producer.ProduceAsync(record);
        return Accepted($"/api/telemetry/{req.DeviceId}", record);
    }

    [HttpGet("devices")]
    public IActionResult GetDevices()
    {
        return Ok(_store.GetDevices());
    }

    [HttpGet("{deviceId}")]
    public IActionResult GetLatest(string deviceId)
    {
        var record = _store.GetLatest(deviceId);
        if (record == null)
        {
            return NotFound(new { error = "Device not found" });
        }
        return Ok(record);
    }
}
