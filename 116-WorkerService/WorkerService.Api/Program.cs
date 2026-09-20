using Microsoft.EntityFrameworkCore;
using WorkerService.Api.Data;
using WorkerService.Api.Services;
using WorkerService.Api.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite("Data Source=workerservice.db"));
builder.Services.AddSingleton<IEmailQueue, EmailQueueService>(); // Singleton vì Channel cần sống suốt app
builder.Services.AddScoped<IJobService, JobService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register Workers
builder.Services.AddHostedService<EmailQueueWorker>();
builder.Services.AddHostedService<ReportGeneratorWorker>();
builder.Services.AddHostedService<JobStatusMonitor>();

var app = builder.Build();

// Ensure DB is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
