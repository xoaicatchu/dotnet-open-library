using SmartHomeHub.Api.Data;
using SmartHomeHub.Api.Mqtt;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<SmartHomeStore>();

// Register MQTT Broker and Client as Hosted Services
builder.Services.AddHostedService<MqttBrokerHostedService>();
builder.Services.AddSingleton<MqttClientHostedService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<MqttClientHostedService>());
builder.Services.AddSingleton<IMqttPublisher>(provider => provider.GetRequiredService<MqttClientHostedService>());
builder.Services.AddHostedService<MqttDeviceSimulatorHostedService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

public partial class Program { }
