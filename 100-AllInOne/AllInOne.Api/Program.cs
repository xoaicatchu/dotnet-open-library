using EasyCaching.InMemory;
using FluentValidation;
using Hangfire;
using Hangfire.MemoryStorage;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Savorboard.CAP.InMemoryMessageQueue;
using Serilog;
using AllInOne.Api.Data;
using AllInOne.Api.Models;
using AllInOne.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Serilog (Library 38)
builder.Host.UseSerilog((ctx, lc) => lc
    .WriteTo.Console()
    .MinimumLevel.Information());

// Controllers & Swagger (Libraries 46, 47)
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// EF Core SQLite (Library 19)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=allinone.db"));

// MediatR (Library 04)
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// AutoMapper (Library 31)
builder.Services.AddAutoMapper(typeof(OrderMappingProfile));

// FluentValidation (Library 33)
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderRequestValidator>();

// FusionCache (Library 27)
builder.Services.AddFusionCache();

// EasyCaching (Library 28)
builder.Services.AddEasyCaching(options =>
{
    options.UseInMemory("default");
});

// MassTransit In-Memory (Library 06)
builder.Services.AddMassTransit(x =>
{
    x.UsingInMemory((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });
});

// CAP In-Memory (Library 07)
builder.Services.AddCap(x =>
{
    x.UseInMemoryStorage();
    x.UseInMemoryMessageQueue();
});

// Hangfire In-Memory (Library 16)
builder.Services.AddHangfire(config => config.UseMemoryStorage());
builder.Services.AddHangfireServer();

// Application Services (Libraries 48, 49, 50, 51, 52...)
builder.Services.AddScoped<OrderDataService>();
builder.Services.AddSingleton<OrderStateMachineService>();
builder.Services.AddSingleton<OrderDocumentService>();
builder.Services.AddScoped<OrderMessagingService>();

var app = builder.Build();

// Ensure DB is created & Seeded with Bogus (Library 41)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    AppDbContext.Seed(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();

public partial class Program;
