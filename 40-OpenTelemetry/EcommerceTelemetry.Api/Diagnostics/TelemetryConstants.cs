using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace EcommerceTelemetry.Api.Diagnostics;

public static class TelemetryConstants
{
    public const string ServiceName = "EcommerceTelemetry.Api";
    public const string ServiceVersion = "1.0.0";
    public const string ActivitySourceName = "Ecommerce.Orders";
    public const string MeterName = "Ecommerce.Metrics";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName, ServiceVersion);
    public static readonly Meter Meter = new(MeterName, ServiceVersion);

    public static readonly Counter<long> OrdersCounter = Meter.CreateCounter<long>(
        name: "ecommerce_orders_total",
        unit: "{orders}",
        description: "Total number of processed e-commerce orders");

    public static readonly Histogram<double> OrderDurationHistogram = Meter.CreateHistogram<double>(
        name: "ecommerce_order_duration_ms",
        unit: "ms",
        description: "Duration of order processing in milliseconds");

    public static readonly UpDownCounter<long> ActiveCheckouts = Meter.CreateUpDownCounter<long>(
        name: "ecommerce_active_checkouts",
        unit: "{checkouts}",
        description: "Number of currently active checkout operations");
}
