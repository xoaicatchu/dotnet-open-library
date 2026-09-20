using System.Text;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Client;
using SmartHomeHub.Api.Models;

namespace SmartHomeHub.Api.Mqtt;

public sealed class MqttDeviceSimulatorHostedService : IHostedService
{
    private readonly ILogger<MqttDeviceSimulatorHostedService> _logger;
    private readonly IConfiguration _configuration;
    private readonly MqttFactory _mqttFactory = new();
    private IMqttClient? _mqttClient;
    private int _stopRequested;

    public MqttDeviceSimulatorHostedService(
        ILogger<MqttDeviceSimulatorHostedService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var port = _configuration.GetValue<int>("MqttBroker:Port", 18883);
        _mqttClient = _mqttFactory.CreateMqttClient();
        _mqttClient.ApplicationMessageReceivedAsync += HandleCommandAsync;

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer("localhost", port)
            .WithClientId("SmartHomeHub_DeviceSimulator")
            .WithCleanSession()
            .Build();

        try
        {
            await _mqttClient.ConnectAsync(options, cancellationToken);

            var subscribeOptions = _mqttFactory.CreateSubscribeOptionsBuilder()
                .WithTopicFilter(filter => filter.WithTopic("home/+/command"))
                .Build();

            await _mqttClient.SubscribeAsync(subscribeOptions, cancellationToken);
            _logger.LogInformation("Device simulator subscribed to 'home/+/command'.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start MQTT device simulator.");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _stopRequested, 1) != 0)
        {
            return;
        }

        if (_mqttClient == null)
        {
            return;
        }

        await _mqttClient.DisconnectAsync(new MqttClientDisconnectOptionsBuilder().Build(), cancellationToken);
        _mqttClient.Dispose();
        _logger.LogInformation("MQTT device simulator disconnected.");
    }

    private async Task HandleCommandAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
    {
        var parts = eventArgs.ApplicationMessage.Topic.Split('/');
        if (parts.Length != 3 || parts[0] != "home" || parts[2] != "command")
        {
            return;
        }

        var deviceId = parts[1];
        var payload = Encoding.UTF8.GetString(eventArgs.ApplicationMessage.PayloadSegment);
        var command = JsonSerializer.Deserialize<DeviceCommand>(payload);
        if (command == null || string.IsNullOrWhiteSpace(command.Command) || _mqttClient == null)
        {
            _logger.LogWarning("Ignored invalid command for simulated device {DeviceId}.", deviceId);
            return;
        }

        var state = new DeviceState
        {
            DeviceId = deviceId,
            DeviceType = command.DeviceType,
            Payload = command.Command,
            LastUpdated = DateTime.UtcNow
        };

        var stateMessage = new MqttApplicationMessageBuilder()
            .WithTopic($"home/{deviceId}/state")
            .WithPayload(JsonSerializer.Serialize(state))
            .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        await _mqttClient.PublishAsync(stateMessage);
        _logger.LogInformation("Simulated device {DeviceId} reported state.", deviceId);
    }
}
