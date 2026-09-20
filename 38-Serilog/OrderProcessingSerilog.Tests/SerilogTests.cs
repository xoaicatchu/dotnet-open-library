using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using OrderProcessingSerilog.Api.Models;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace OrderProcessingSerilog.Tests;

public class TestLogSink : ILogEventSink
{
    public static readonly ConcurrentBag<LogEvent> EmittedEvents = new();

    public void Emit(LogEvent logEvent)
    {
        EmittedEvents.Add(logEvent);
    }

    public static void Clear() => EmittedEvents.Clear();
}

public class SerilogTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SerilogTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSerilog((sp, configuration) =>
                {
                    configuration
                        .MinimumLevel.Debug()
                        .Enrich.FromLogContext()
                        .WriteTo.Sink(new TestLogSink());
                });
            });
        }).CreateClient();
    }

    [Fact]
    public async Task CreateOrder_LogsInformationWithStructuredProperties()
    {
        TestLogSink.Clear();
        var request = new CreateOrderRequest("cust_alpha_01", 150.00m, "Noise-Cancelling Headphones");
        var response = await _client.PostAsJsonAsync("/api/orders", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var events = TestLogSink.EmittedEvents.ToList();
        var orderEvent = events.FirstOrDefault(e => e.MessageTemplate.Text.Contains("Successfully created order"));
        Assert.NotNull(orderEvent);
        Assert.Equal(LogEventLevel.Information, orderEvent.Level);
        Assert.True(orderEvent.Properties.ContainsKey("OrderId"));
        Assert.True(orderEvent.Properties.ContainsKey("CustomerId"));
        Assert.True(orderEvent.Properties.ContainsKey("Amount"));
        Assert.Equal("\"cust_alpha_01\"", orderEvent.Properties["CustomerId"].ToString());
    }

    [Fact]
    public async Task CreateOrder_EnrichesLogWithCorrelationId()
    {
        TestLogSink.Clear();
        var request = new CreateOrderRequest("cust_beta_02", 75.00m, "Wireless Mouse");
        var response = await _client.PostAsJsonAsync("/api/orders", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var events = TestLogSink.EmittedEvents.ToList();
        var orderEvent = events.FirstOrDefault(e => e.MessageTemplate.Text.Contains("Successfully created order"));
        Assert.NotNull(orderEvent);
        Assert.True(orderEvent.Properties.ContainsKey("CorrelationId"));
    }

    [Fact]
    public async Task CreateOrder_InvalidRequest_LogsWarning()
    {
        TestLogSink.Clear();
        var request = new CreateOrderRequest("", -5.00m, "Invalid Item");
        var response = await _client.PostAsJsonAsync("/api/orders", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var events = TestLogSink.EmittedEvents.ToList();
        var warningEvent = events.FirstOrDefault(e => e.Level == LogEventLevel.Warning);
        Assert.NotNull(warningEvent);
        Assert.Contains("Invalid order request received", warningEvent.MessageTemplate.Text);
    }

    [Fact]
    public async Task GetById_LogsDiagnosticSearchEvent()
    {
        TestLogSink.Clear();
        var response = await _client.GetAsync("/api/orders/ORD-SEARCH-TEST");

        var events = TestLogSink.EmittedEvents.ToList();
        var searchEvent = events.FirstOrDefault(e => e.MessageTemplate.Text.Contains("Searching for order with ID"));
        Assert.NotNull(searchEvent);
        Assert.True(searchEvent.Properties.ContainsKey("OrderId"));
    }

    [Fact]
    public async Task FailOrder_LogsErrorWithExceptionDetails()
    {
        TestLogSink.Clear();
        var response = await _client.PostAsync("/api/orders/ORD-FAIL-01/fail", null);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var events = TestLogSink.EmittedEvents.ToList();
        var errorEvent = events.FirstOrDefault(e => e.Level == LogEventLevel.Error && e.MessageTemplate.Text.Contains("payment provider timeout"));
        Assert.NotNull(errorEvent);
        Assert.NotNull(errorEvent.Exception);
        Assert.IsType<InvalidOperationException>(errorEvent.Exception);
        Assert.Contains("payment provider timeout", errorEvent.MessageTemplate.Text);
    }
}
