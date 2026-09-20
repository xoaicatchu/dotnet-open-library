using Akka.Actor;
using Akka.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrderWorkflowActors.Api.Actors;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddAkka("OrderActorSystem", (configurationBuilder, serviceProvider) =>
{
    configurationBuilder.WithActors((system, registry, resolver) =>
    {
        var orderManager = system.ActorOf(Props.Create(() => new OrderManagerActor()), "orderManager");
        registry.Register<OrderManagerActor>(orderManager);
    });
});

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
