using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NServiceBus;
using OrderBilling.Api.Data;
using OrderBilling.Api.Messages;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<InvoiceStore>();

builder.Host.UseNServiceBus(context =>
{
    var endpointConfiguration = new EndpointConfiguration("OrderBilling.Api");
    endpointConfiguration.UseSerialization<SystemJsonSerializer>();
    var transport = endpointConfiguration.UseTransport<LearningTransport>();
    
    // Auto route BillOrderCommand locally to this endpoint
    var routing = transport.Routing();
    routing.RouteToEndpoint(typeof(BillOrderCommand), "OrderBilling.Api");
    
    endpointConfiguration.SendFailedMessagesTo("error");
    endpointConfiguration.AuditProcessedMessagesTo("audit");
    endpointConfiguration.EnableInstallers();

    return endpointConfiguration;
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
