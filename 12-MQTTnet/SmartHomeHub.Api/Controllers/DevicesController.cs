using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using SmartHomeHub.Api.Data;
using SmartHomeHub.Api.Models;
using SmartHomeHub.Api.Mqtt;

namespace SmartHomeHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DevicesController : ControllerBase
{
    private readonly SmartHomeStore _store;
    private readonly IMqttPublisher _mqttPublisher;

    public DevicesController(SmartHomeStore store, IMqttPublisher mqttPublisher)
    {
        _store = store;
        _mqttPublisher = mqttPublisher;
    }

    [HttpPost("{deviceId}/command")]
    public async Task<IActionResult> SendCommand(string deviceId, [FromBody] DeviceCommand command)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return BadRequest("Device ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.Command))
        {
            return BadRequest("Command is required.");
        }

        var topic = $"home/{deviceId}/command";
        var published = await _mqttPublisher.PublishAsync(topic, JsonSerializer.Serialize(command));

        if (!published)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "MQTT broker is unavailable.");
        }

        return Accepted();
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(_store.GetAllDevices());
    }

    [HttpGet("{deviceId}")]
    public IActionResult GetById(string deviceId)
    {
        var device = _store.GetDevice(deviceId);
        return device != null ? Ok(device) : NotFound();
    }
}
