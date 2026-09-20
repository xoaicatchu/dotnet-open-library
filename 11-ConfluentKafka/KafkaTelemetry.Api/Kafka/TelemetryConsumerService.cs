using System.Text.Json;
using Confluent.Kafka;
using KafkaTelemetry.Api.Data;
using KafkaTelemetry.Api.Models;
using Microsoft.Extensions.Options;

namespace KafkaTelemetry.Api.Kafka;

public class TelemetryConsumerService : BackgroundService
{
    private readonly KafkaOptions _options;
    private readonly TelemetryStore _store;
    private readonly ILogger<TelemetryConsumerService> _logger;

    public TelemetryConsumerService(IOptions<KafkaOptions> options, TelemetryStore store, ILogger<TelemetryConsumerService> logger)
    {
        _options = options.Value;
        _store = store;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnableConsumer)
        {
            _logger.LogInformation("Kafka consumer is disabled.");
            return;
        }

        await Task.Yield(); // Ensure start immediately

        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            AllowAutoCreateTopics = true
        };

        try
        {
            using var consumer = new ConsumerBuilder<string, string>(config).Build();
            consumer.Subscribe(_options.Topic);

            _logger.LogInformation("Started Kafka consumer on {Topic}...", _options.Topic);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);
                    if (result?.Message?.Value != null)
                    {
                        var record = JsonSerializer.Deserialize<TelemetryRecord>(result.Message.Value);
                        if (record != null)
                        {
                            _store.Save(record);
                            _logger.LogDebug("Saved telemetry for {DeviceId}", record.DeviceId);
                        }
                    }
                }
                catch (ConsumeException e)
                {
                    _logger.LogError(e, "Error consuming message.");
                }
            }
            
            consumer.Close();
        }
        catch (KafkaException e)
        {
            _logger.LogWarning(e, "Kafka broker is unreachable. Consumer stopping to avoid crash.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Consumer cancelled.");
        }
    }
}
