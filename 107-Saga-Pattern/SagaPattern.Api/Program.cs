using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MassTransit;
using SagaPattern.Api.Sagas;
using SagaPattern.Api.Consumers;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddMassTransit(x => {
    x.AddSagaStateMachine<OrderSaga, OrderSagaState>()
        .InMemoryRepository();
    
    x.AddConsumer<PaymentConsumer>();
    x.AddConsumer<InventoryConsumer>();
    x.AddConsumer<ShippingConsumer>();
    
    x.UsingInMemory((ctx, cfg) => {
        cfg.ConfigureEndpoints(ctx);
    });
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.MapControllers();
app.Run();
public partial class Program {}
