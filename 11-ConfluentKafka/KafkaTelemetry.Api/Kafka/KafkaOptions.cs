namespace KafkaTelemetry.Api.Kafka;

public class KafkaOptions
{
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string Topic { get; set; } = "device-telemetry";
    public string GroupId { get; set; } = "telemetry-group";
    public bool EnableConsumer { get; set; } = true;
}
