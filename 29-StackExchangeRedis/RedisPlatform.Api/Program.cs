using RedisPlatform.Api.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var redisConnStr = builder.Configuration.GetConnectionString("Redis");

if (!string.IsNullOrWhiteSpace(redisConnStr) && !redisConnStr.Contains("YOUR_REDIS"))
{
    // Real Redis with StackExchange.Redis
    builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
        ConnectionMultiplexer.Connect(redisConnStr));
    builder.Services.AddSingleton<IRedisService, StackExchangeRedisService>();
}
else
{
    // In-memory fallback for standalone testing / CI
    builder.Services.AddSingleton<IRedisService, InMemoryRedisService>();
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();

public partial class Program;
