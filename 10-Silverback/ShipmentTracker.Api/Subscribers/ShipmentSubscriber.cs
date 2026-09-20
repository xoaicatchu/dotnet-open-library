using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ShipmentTracker.Api.Data;
using ShipmentTracker.Api.Messages;
using Silverback.Messaging.Subscribers;

namespace ShipmentTracker.Api.Subscribers;

public class ShipmentSubscriber
{
    private readonly ShipmentStore _store;
    private readonly ILogger<ShipmentSubscriber> _logger;

    public ShipmentSubscriber(ShipmentStore store, ILogger<ShipmentSubscriber> logger)
    {
        _store = store;
        _logger = logger;
    }

    [Subscribe]
    public async Task OnShipmentCreated(ShipmentCreatedEvent message)
    {
        _logger.LogInformation("Shipment created event received: {Id}", message.ShipmentId);
        // Can perform side-effects, audit, notification simulation
        await Task.CompletedTask;
    }

    [Subscribe]
    public async Task OnUpdateStatus(UpdateShipmentStatusCommand message)
    {
        _logger.LogInformation("Updating shipment {Id} status to {Status}", message.ShipmentId, message.NewStatus);
        _store.UpdateStatus(message.ShipmentId, message.NewStatus);
        await Task.CompletedTask;
    }
}
