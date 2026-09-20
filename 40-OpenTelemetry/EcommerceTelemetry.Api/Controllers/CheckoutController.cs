using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using EcommerceTelemetry.Api.Diagnostics;
using EcommerceTelemetry.Api.Models;

namespace EcommerceTelemetry.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CheckoutController : ControllerBase
{
    private static long _totalOrders;
    private static decimal _totalRevenue;

    /// <summary>
    /// Processes checkout and emits custom OpenTelemetry traces and metrics.
    /// </summary>
    [HttpPost("process")]
    [ProducesResponseType(typeof(CheckoutResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult ProcessCheckout([FromBody] CheckoutRequest request)
    {
        using var activity = TelemetryConstants.ActivitySource.StartActivity("ProcessCheckout", ActivityKind.Server);
        TelemetryConstants.ActiveCheckouts.Add(1);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            activity?.SetTag("customer.id", request.CustomerId);
            activity?.SetTag("payment.method", request.PaymentMethod);
            activity?.SetTag("order.amount", request.Amount);

            if (string.IsNullOrWhiteSpace(request.CustomerId) || request.Amount <= 0)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Invalid customer ID or amount.");
                activity?.AddEvent(new ActivityEvent("CheckoutValidationFailed"));
                TelemetryConstants.OrdersCounter.Add(1, new KeyValuePair<string, object?>("status", "failed"));

                return BadRequest(new { message = "CustomerId is required and Amount must be greater than 0." });
            }

            // Simulate checkout processing
            var orderId = $"ORD-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}";
            Interlocked.Increment(ref _totalOrders);
            _totalRevenue += request.Amount;

            activity?.SetTag("order.id", orderId);
            activity?.SetStatus(ActivityStatusCode.Ok);
            activity?.AddEvent(new ActivityEvent("PaymentAuthorized"));

            stopwatch.Stop();
            TelemetryConstants.OrderDurationHistogram.Record(
                stopwatch.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("payment.method", request.PaymentMethod));

            TelemetryConstants.OrdersCounter.Add(
                1,
                new KeyValuePair<string, object?>("status", "success"),
                new KeyValuePair<string, object?>("payment.method", request.PaymentMethod));

            var currentTraceId = Activity.Current?.TraceId.ToString() ?? activity?.TraceId.ToString() ?? "unknown";
            var currentSpanId = Activity.Current?.SpanId.ToString() ?? activity?.SpanId.ToString() ?? "unknown";

            return Ok(new CheckoutResponse(
                OrderId: orderId,
                Status: "Completed",
                TraceId: currentTraceId,
                SpanId: currentSpanId,
                ProcessedAt: DateTime.UtcNow
            ));
        }
        finally
        {
            TelemetryConstants.ActiveCheckouts.Add(-1);
        }
    }

    /// <summary>
    /// Returns current W3C trace context propagated through headers.
    /// </summary>
    [HttpGet("trace")]
    [ProducesResponseType(typeof(TraceContextResponse), StatusCodes.Status200OK)]
    public IActionResult GetTraceContext()
    {
        var current = Activity.Current;
        return Ok(new TraceContextResponse(
            TraceId: current?.TraceId.ToString() ?? "none",
            SpanId: current?.SpanId.ToString() ?? "none",
            ParentSpanId: current?.ParentSpanId.ToString()
        ));
    }

    /// <summary>
    /// Returns a summary snapshot of metrics.
    /// </summary>
    [HttpGet("metrics-summary")]
    [ProducesResponseType(typeof(MetricsSummaryResponse), StatusCodes.Status200OK)]
    public IActionResult GetMetricsSummary()
    {
        return Ok(new MetricsSummaryResponse(
            TotalOrders: Interlocked.Read(ref _totalOrders),
            TotalRevenue: _totalRevenue
        ));
    }
}
