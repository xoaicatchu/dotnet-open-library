using System.Text;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Client;
using SmartHomeHub.Api.Data;
using SmartHomeHub.Api.Models;

namespace SmartHomeHub.Api.Mqtt;

public class MqttClientHostedService : IHostedService, IMqttPublisher
{
    private readonly ILogger<MqttClientHostedService> _logger;
    private readonly IConfiguration _configuration;
    private readonly SmartHomeStore _store;
    private IMqttClient? _mqttClient;
    private readonly MqttFactory _mqttFactory;
    private int _stopRequested;

    public MqttClientHostedService(ILogger<MqttClientHostedService> logger, IConfiguration configuration, SmartHomeStore store)
    {
        _logger = logger;
        _configuration = configuration;
        _store = store;
        _mqttFactory = new MqttFactory();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var port = _configuration.GetValue<int>("MqttBroker:Port", 18883);
        _mqttClient = _mqttFactory.CreateMqttClient();

        var mqttClientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer("localhost", port)
            .WithClientId("SmartHomeHub_InternalClient")
            .WithCleanSession()
            .Build();

        _mqttClient.ApplicationMessageReceivedAsync += e =>
        {
            var topic = e.ApplicationMessage.Topic;
            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
            _logger.LogInformation("Received message on topic {Topic}: {Payload}", topic, payload);

            var parts = topic.Split('/');
            if (parts.Length >= 3 && parts[0] == "home" && parts[2] == "state")
            {
                var deviceId = parts[1];
                var state = JsonSerializer.Deserialize<DeviceState>(payload);
                if (state != null && state.DeviceId == deviceId)
                {
                    _store.UpdateDevice(state.DeviceId, state.DeviceType, state.Payload, state.LastUpdated);
                }
            }

            return Task.CompletedTask;
        };

        try
        {
            await Task.Delay(500, cancellationToken);
            
            await _mqttClient.ConnectAsync(mqttClientOptions, cancellationToken);
            _logger.LogInformation("MQTT client connected.");

            var subscribeOptions = _mqttFactory.CreateSubscribeOptionsBuilder()
                .WithTopicFilter(f => f.WithTopic("home/+/state"))
                .Build();

            await _mqttClient.SubscribeAsync(subscribeOptions, cancellationToken);
            _logger.LogInformation("Subscribed to topic 'home/+/state'.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect MQTT client.");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _stopRequested, 1) != 0)
        {
            return;
        }

        if (_mqttClient != null)
        {
            await _mqttClient.DisconnectAsync(new MqttClientDisconnectOptionsBuilder().Build(), cancellationToken);
            _mqttClient.Dispose();
            _logger.LogInformation("MQTT client disconnected.");
        }
    }

    public async Task<bool> PublishAsync(string topic, string payload)
    {
        if (_mqttClient == null || !_mqttClient.IsConnected)
        {
            _logger.LogWarning("Cannot publish, MQTT client is not connected.");
            return false;
        }

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        await _mqttClient.PublishAsync(message);
        _logger.LogInformation("Published message to {Topic}.", topic);
        return true;
    }
}
