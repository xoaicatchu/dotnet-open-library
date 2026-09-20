using System.Collections.Concurrent;
using SmartHomeHub.Api.Models;

namespace SmartHomeHub.Api.Data;

public class SmartHomeStore
{
    private readonly ConcurrentDictionary<string, DeviceState> _devices = new();

    public void UpdateDevice(string deviceId, string deviceType, string payload, DateTime? lastUpdated = null)
    {
        _devices.AddOrUpdate(deviceId,
            id => new DeviceState { DeviceId = id, DeviceType = deviceType, Payload = payload, LastUpdated = lastUpdated ?? DateTime.UtcNow },
            (id, existing) =>
            {
                existing.Payload = payload;
                if (!string.IsNullOrEmpty(deviceType))
                {
                    existing.DeviceType = deviceType;
                }
                existing.LastUpdated = lastUpdated ?? DateTime.UtcNow;
                return existing;
            });
    }

    public DeviceState? GetDevice(string deviceId)
    {
        return _devices.TryGetValue(deviceId, out var state) ? state : null;
    }

    public IEnumerable<DeviceState> GetAllDevices()
    {
        return _devices.Values;
    }
}
