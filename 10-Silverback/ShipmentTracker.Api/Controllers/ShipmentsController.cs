using Microsoft.AspNetCore.Mvc;
using ShipmentTracker.Api.Data;
using ShipmentTracker.Api.Messages;
using Silverback.Messaging.Publishing;

namespace ShipmentTracker.Api.Controllers;

public record CreateShipmentRequest(string Destination);
public record UpdateStatusRequest(string Status);

[ApiController]
[Route("api/[controller]")]
public class ShipmentsController : ControllerBase
{
    private readonly ShipmentStore _store;
    private readonly IPublisher _publisher;

    public ShipmentsController(ShipmentStore store, IPublisher publisher)
    {
        _store = store;
        _publisher = publisher;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateShipmentRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Destination))
            return BadRequest(new { error = "Destination is required" });

        var shipment = _store.Create(req.Destination);
        await _publisher.PublishAsync(new ShipmentCreatedEvent(shipment.Id, shipment.TrackingCode, shipment.Destination));
        return Created($"/api/shipments/{shipment.Id}", shipment);
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(_store.GetAll());
    }

    [HttpGet("{id:guid}")]
    public IActionResult GetById(Guid id)
    {
        var shipment = _store.GetById(id);
        if (shipment is null) return NotFound();
        return Ok(shipment);
    }

    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest req)
    {
        var shipment = _store.GetById(id);
        if (shipment is null) return NotFound();
        
        if (req.Status is not ("InTransit" or "Delivered" or "Cancelled"))
            return BadRequest(new { error = "Invalid status" });

        await _publisher.PublishAsync(new UpdateShipmentStatusCommand(id, req.Status));
        var updated = _store.GetById(id);
        return Ok(updated);
    }
}
