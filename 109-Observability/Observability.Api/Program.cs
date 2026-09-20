using Microsoft.EntityFrameworkCore;
using Observability.Api.Data;
using Observability.Api.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Serilog;
using System.Diagnostics.Metrics;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, lc) => lc
    .WriteTo.Console()
    .Enrich.WithProperty("Application", "Observability.Api")
    .Enrich.WithProperty("Environment", ctx.HostingEnvironment.EnvironmentName)
    .MinimumLevel.Information());

// OpenTelemetry Tracing & Metrics
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("Observability.Api"))
        .AddSource("Observability.Api")
        .AddAspNetCoreInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddConsoleExporter())  // Console — không cần Jaeger
    .WithMetrics(metrics => metrics
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("Observability.Api"))
        .AddAspNetCoreInstrumentation()
        .AddMeter("Observability.Api")
        .AddPrometheusExporter());  // /metrics endpoint

builder.Services.AddSingleton<AppMetrics>();
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite("Data Source=obs.db"));
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseOpenTelemetryPrometheusScrapingEndpoint(); // /metrics

if (app.Environment.IsDevelopment()) 
{ 
    app.UseSwagger(); 
    app.UseSwaggerUI(); 
}
app.MapControllers();
app.Run();

public partial class Program { }
