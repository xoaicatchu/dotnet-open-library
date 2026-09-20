using MQTTnet;
using MQTTnet.Server;

namespace SmartHomeHub.Api.Mqtt;

public class MqttBrokerHostedService : IHostedService
{
    private readonly ILogger<MqttBrokerHostedService> _logger;
    private readonly IConfiguration _configuration;
    private MqttServer? _mqttServer;
    private int _stopRequested;

    public MqttBrokerHostedService(ILogger<MqttBrokerHostedService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var port = _configuration.GetValue<int>("MqttBroker:Port", 18883);
        
        var mqttFactory = new MqttFactory();
        var mqttServerOptions = mqttFactory.CreateServerOptionsBuilder()
            .WithDefaultEndpoint()
            .WithDefaultEndpointPort(port)
            .Build();

        _mqttServer = mqttFactory.CreateMqttServer(mqttServerOptions);
        
        _mqttServer.ValidatingConnectionAsync += e =>
        {
            _logger.LogInformation("Client '{ClientId}' connecting.", e.ClientId);
            return Task.CompletedTask;
        };

        _mqttServer.ClientConnectedAsync += e =>
        {
            _logger.LogInformation("Client '{ClientId}' connected.", e.ClientId);
            return Task.CompletedTask;
        };
        
        await _mqttServer.StartAsync();
        _logger.LogInformation("Embedded MQTT broker started on port {Port}.", port);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _stopRequested, 1) != 0)
        {
            return;
        }

        if (_mqttServer != null)
        {
            try
            {
                await _mqttServer.StopAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Handled exception during MQTT server shutdown.");
            }
            finally
            {
                _mqttServer.Dispose();
            }
            _logger.LogInformation("Embedded MQTT broker stopped.");
        }
    }
}
