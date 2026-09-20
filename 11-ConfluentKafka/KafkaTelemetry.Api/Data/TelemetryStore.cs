using System.Collections.Concurrent;
using KafkaTelemetry.Api.Models;

namespace KafkaTelemetry.Api.Data;

public class TelemetryStore
{
    // Store latest telemetry per device
    private readonly ConcurrentDictionary<string, TelemetryRecord> _store = new();

    public void Save(TelemetryRecord record)
    {
        _store.AddOrUpdate(record.DeviceId, record, (_, _) => record);
    }

    public IEnumerable<string> GetDevices()
    {
        return _store.Keys;
    }

    public TelemetryRecord? GetLatest(string deviceId)
    {
        if (_store.TryGetValue(deviceId, out var record))
        {
            return record;
        }
        return null;
    }
}
