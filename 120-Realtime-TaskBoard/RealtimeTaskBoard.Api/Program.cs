using Microsoft.EntityFrameworkCore;
using RealtimeTaskBoard.Api.Features.Activities;
using RealtimeTaskBoard.Api.Features.Boards;
using RealtimeTaskBoard.Api.Features.Tasks;
using RealtimeTaskBoard.Api.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Database — PostgreSQL primary, SQLite fallback if PG unavailable
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var useSqlite = builder.Configuration.GetValue<bool>("UseSqlite");

if (useSqlite || string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite("Data Source=taskboard.db"));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString));
}

// SignalR
builder.Services.AddSignalR();

// Services
builder.Services.AddScoped<ActivityService>();

// Controllers + Swagger
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS for Angular dev server
builder.Services.AddCors(options =>
{
    options.AddPolicy("Angular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // Required for SignalR
    });
});

var app = builder.Build();

// Auto-migrate in Development (skip for InMemory test provider)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
    {
        await db.Database.EnsureCreatedAsync();
    }

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Angular");
app.MapControllers();

// Map SignalR Hubs
app.MapHub<BoardHub>("/hubs/board");
app.MapHub<TaskHub>("/hubs/task");
app.MapHub<ActivityHub>("/hubs/activity");

app.Run();

public partial class Program { }
