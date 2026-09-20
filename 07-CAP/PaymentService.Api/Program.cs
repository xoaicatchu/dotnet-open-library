using DotNetCore.CAP;
using Savorboard.CAP.InMemoryMessageQueue;
using PaymentService.Api.Data;
using PaymentService.Api.Subscribers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<PaymentStore>();
builder.Services.AddTransient<PaymentEventSubscriber>();

builder.Services.AddCap(x =>
{
    x.UseInMemoryStorage();
    x.UseInMemoryMessageQueue();
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

public partial class Program;
