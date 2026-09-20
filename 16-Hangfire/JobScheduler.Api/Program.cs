using Hangfire;
using Hangfire.MemoryStorage;
using JobScheduler.Api.Data;
using JobScheduler.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<JobAuditStore>();
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddTransient<IReportService, ReportService>();

builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseMemoryStorage());

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 2;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [] // Allow local / testing access without auth
});

app.MapControllers();
app.Run();

public partial class Program { }
