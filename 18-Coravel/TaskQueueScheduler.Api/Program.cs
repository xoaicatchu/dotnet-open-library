using TaskQueueScheduler.Api.Data;
using TaskQueueScheduler.Api.Invocables;
using Coravel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<TaskAuditStore>();
builder.Services.AddTransient<HeartbeatInvocable>();
builder.Services.AddTransient<DataProcessingInvocable>();

builder.Services.AddScheduler();
builder.Services.AddQueue();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Services.UseScheduler(scheduler =>
{
    scheduler.Schedule<HeartbeatInvocable>()
        .EverySeconds(2)
        .PreventOverlapping(nameof(HeartbeatInvocable));
});

app.MapControllers();
app.Run();

public partial class Program { }
