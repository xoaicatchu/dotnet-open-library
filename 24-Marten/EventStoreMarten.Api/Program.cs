using EventStoreMarten.Api.Domain;
using EventStoreMarten.Api.Services;
using Marten;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var connectionString = builder.Configuration.GetConnectionString("PostgresConnection");

if (!string.IsNullOrWhiteSpace(connectionString) && !connectionString.Contains("YOUR_POSTGRES"))
{
    // Production Marten with PostgreSQL
    builder.Services.AddMarten(options =>
    {
        options.Connection(connectionString);
        options.Projections.LiveStreamAggregation<BankAccount>();
    }).UseLightweightSessions();

    builder.Services.AddScoped<IBankAccountStore, MartenBankAccountStore>();
}
else
{
    // Standalone / In-memory store for local testing without PostgreSQL
    builder.Services.AddSingleton<IBankAccountStore, InMemoryBankAccountStore>();
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
