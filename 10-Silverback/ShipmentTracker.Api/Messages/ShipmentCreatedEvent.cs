using System;

namespace ShipmentTracker.Api.Messages;

public record ShipmentCreatedEvent(Guid ShipmentId, string TrackingCode, string Destination);
