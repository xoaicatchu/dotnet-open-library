using System;

namespace ShipmentTracker.Api.Messages;

public record UpdateShipmentStatusCommand(Guid ShipmentId, string NewStatus);
