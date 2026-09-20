namespace SmartHomeHub.Api.Mqtt;

public interface IMqttPublisher
{
    Task<bool> PublishAsync(string topic, string payload);
}
