using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ShipmentTracker.Api.Models;

namespace ShipmentTracker.Api.Data;

public class ShipmentStore
{
    private readonly ConcurrentDictionary<Guid, Shipment> _shipments = new();

    public Shipment Create(string destination)
    {
        var id = Guid.NewGuid();
        var shipment = new Shipment
        {
            Id = id,
            TrackingCode = $"TRK-{Random.Shared.Next(100000, 999999)}",
            Destination = destination,
            Status = "Created",
            CreatedAt = DateTime.UtcNow
        };
        _shipments.TryAdd(id, shipment);
        return shipment;
    }

    public Shipment? GetById(Guid id)
    {
        return _shipments.TryGetValue(id, out var shipment) ? shipment : null;
    }

    public IEnumerable<Shipment> GetAll()
    {
        return _shipments.Values.ToList();
    }

    public void UpdateStatus(Guid id, string status)
    {
        if (_shipments.TryGetValue(id, out var shipment))
        {
            shipment.Status = status;
            shipment.UpdatedAt = DateTime.UtcNow;
        }
    }
}
