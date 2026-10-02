using AspireDemo.Worker;

var builder = Host.CreateApplicationBuilder(args);

// ServiceDefaults: OpenTelemetry + health checks (Worker Service cũng được monitor)
builder.AddServiceDefaults();

// Aspire Redis integration
builder.AddRedisClient("redis");

// Background worker
builder.Services.AddHostedService<TaskProcessorWorker>();

var host = builder.Build();
host.Run();
