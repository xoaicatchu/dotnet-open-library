namespace EcommerceTelemetry.Api.Models;

public record CheckoutRequest(
    string CustomerId,
    decimal Amount,
    string PaymentMethod
);

public record CheckoutResponse(
    string OrderId,
    string Status,
    string TraceId,
    string SpanId,
    DateTime ProcessedAt
);

public record TraceContextResponse(
    string TraceId,
    string SpanId,
    string? ParentSpanId
);

public record MetricsSummaryResponse(
    long TotalOrders,
    decimal TotalRevenue
);
