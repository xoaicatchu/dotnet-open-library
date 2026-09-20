using InventoryService.Api.Data;
using InventoryService.Api.Messages;
using Rebus.Config;
using Rebus.Routing.TypeBased;
using Rebus.Transport.InMem;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<InventoryStore>();

// Shared in-memory network for Rebus
var inMemNetwork = new InMemNetwork();

builder.Services.AddRebus(configure => configure
    .Transport(t => t.UseInMemoryTransport(inMemNetwork, "inventory-queue"))
    .Routing(r => r.TypeBased()
        .Map<AddStockCommand>("inventory-queue")
        .Map<RemoveStockCommand>("inventory-queue")));

// Auto-register handlers from this assembly
builder.Services.AutoRegisterHandlersFromAssemblyOf<Program>();

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

public partial class Program { }
