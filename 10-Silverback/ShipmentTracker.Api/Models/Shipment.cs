using System;

namespace ShipmentTracker.Api.Models;

public class Shipment
{
    public Guid Id { get; set; }
    public string TrackingCode { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public string Status { get; set; } = "Created";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
