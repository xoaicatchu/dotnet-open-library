using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using EcommerceTelemetry.Api.Diagnostics;
using EcommerceTelemetry.Api.Models;

namespace EcommerceTelemetry.Tests;

public class OpenTelemetryTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly HttpClient _client;
    private readonly ActivityListener _activityListener;
    private readonly MeterListener _meterListener;
    private readonly ConcurrentBag<Activity> _recordedActivities = new();
    private readonly ConcurrentBag<(Instrument Instrument, long Value, TagList Tags)> _recordedMeasurements = new();

    public OpenTelemetryTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();

        _activityListener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == TelemetryConstants.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => _recordedActivities.Add(activity)
        };
        ActivitySource.AddActivityListener(_activityListener);

        _meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == TelemetryConstants.MeterName)
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };
        _meterListener.SetMeasurementEventCallback<long>((instrument, value, tags, state) =>
        {
            _recordedMeasurements.Add((instrument, value, new TagList(tags)));
        });
        _meterListener.Start();
    }

    public void Dispose()
    {
        _activityListener.Dispose();
        _meterListener.Dispose();
    }

    [Fact]
    public async Task Checkout_SuccessfulOrder_EmitsActivityWithCustomTags()
    {
        var request = new CheckoutRequest("cust_alice_101", 199.99m, "CreditCard");
        var response = await _client.PostAsJsonAsync("/api/checkout/process", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<CheckoutResponse>();
        Assert.NotNull(result);
        Assert.Equal("Completed", result.Status);
        Assert.False(string.IsNullOrWhiteSpace(result.TraceId));

        var activity = _recordedActivities.FirstOrDefault(a => a.OperationName == "ProcessCheckout");
        Assert.NotNull(activity);
        Assert.Equal("cust_alice_101", activity.GetTagItem("customer.id"));
        Assert.Equal(199.99m, (decimal)(activity.GetTagItem("order.amount") ?? 0m));
        Assert.Equal(ActivityStatusCode.Ok, activity.Status);
    }

    [Fact]
    public async Task Checkout_NegativeAmount_SetsActivityErrorStatus()
    {
        var request = new CheckoutRequest("cust_bob_202", -50m, "PayPal");
        var response = await _client.PostAsJsonAsync("/api/checkout/process", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var activity = _recordedActivities.FirstOrDefault(a => (string?)a.GetTagItem("customer.id") == "cust_bob_202");
        Assert.NotNull(activity);
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
    }

    [Fact]
    public async Task Checkout_SuccessfulOrder_RecordsOrderCounter()
    {
        var request = new CheckoutRequest("cust_charlie_303", 50.00m, "DebitCard");
        var response = await _client.PostAsJsonAsync("/api/checkout/process", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var measurement = _recordedMeasurements.FirstOrDefault(m => m.Instrument.Name == "ecommerce_orders_total");
        Assert.NotNull(measurement.Instrument);
        Assert.Equal(1, measurement.Value);
    }

    [Fact]
    public async Task GetTrace_ReturnsW3CTraceAndSpanIds()
    {
        var response = await _client.GetAsync("/api/checkout/trace");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<TraceContextResponse>();
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.TraceId));
        Assert.False(string.IsNullOrWhiteSpace(result.SpanId));
    }

    [Fact]
    public async Task GetTrace_WithTraceparentHeader_PropagatesTraceId()
    {
        var customTraceId = "4bf92f3577b34da6a3ce929d0e0e4736";
        var customSpanId = "00f067aa0ba902b7";
        var traceparent = $"00-{customTraceId}-{customSpanId}-01";

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/checkout/trace");
        request.Headers.Add("traceparent", traceparent);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<TraceContextResponse>();
        Assert.NotNull(result);
        Assert.Equal(customTraceId, result.TraceId);
        Assert.Equal(customSpanId, result.ParentSpanId);
    }
}
