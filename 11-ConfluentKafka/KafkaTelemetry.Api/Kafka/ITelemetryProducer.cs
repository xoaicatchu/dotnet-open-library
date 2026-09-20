using KafkaTelemetry.Api.Models;

namespace KafkaTelemetry.Api.Kafka;

public interface ITelemetryProducer
{
    Task ProduceAsync(TelemetryRecord record, CancellationToken ct = default);
}
