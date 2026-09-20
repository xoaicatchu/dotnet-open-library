using System.Text.Json;
using Confluent.Kafka;
using KafkaTelemetry.Api.Models;
using Microsoft.Extensions.Options;

namespace KafkaTelemetry.Api.Kafka;

public class ConfluentKafkaProducer : ITelemetryProducer, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly string _topic;
    private readonly ILogger<ConfluentKafkaProducer> _logger;

    public ConfluentKafkaProducer(IOptions<KafkaOptions> options, ILogger<ConfluentKafkaProducer> logger)
    {
        _logger = logger;
        _topic = options.Value.Topic;

        var config = new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            AllowAutoCreateTopics = true
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task ProduceAsync(TelemetryRecord record, CancellationToken ct = default)
    {
        var message = new Message<string, string>
        {
            Key = record.DeviceId,
            Value = JsonSerializer.Serialize(record)
        };

        try
        {
            var result = await _producer.ProduceAsync(_topic, message, ct);
            _logger.LogInformation("Delivered telemetry for {DeviceId} to {TopicPartitionOffset}", record.DeviceId, result.TopicPartitionOffset);
        }
        catch (ProduceException<string, string> e)
        {
            _logger.LogError(e, "Delivery failed for {DeviceId}", record.DeviceId);
            throw;
        }
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(2));
        _producer.Dispose();
    }
}
