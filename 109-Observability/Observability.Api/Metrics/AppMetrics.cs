using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Observability.Api.Metrics;

public class AppMetrics
{
    private readonly Counter<long> _requestCounter;
    private readonly Histogram<double> _requestDuration;
    private readonly Counter<long> _productCreatedCounter;
    private readonly UpDownCounter<long> _activeProductsCounter;
    
    public AppMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("Observability.Api");
        _requestCounter = meter.CreateCounter<long>("api.requests.total", description: "Total API requests");
        _requestDuration = meter.CreateHistogram<double>("api.request.duration_ms", description: "Request duration in ms");
        _productCreatedCounter = meter.CreateCounter<long>("products.created.total", description: "Total products created");
        _activeProductsCounter = meter.CreateUpDownCounter<long>("products.active.count", description: "Active products count");
    }
    
    public void RecordRequest(string endpoint, string method) =>
        _requestCounter.Add(1, new TagList {{ "endpoint", endpoint }, { "method", method }});
    
    public void RecordDuration(double ms, string endpoint) =>
        _requestDuration.Record(ms, new TagList {{ "endpoint", endpoint }});
    
    public void RecordProductCreated() => _productCreatedCounter.Add(1);
    public void IncrementActiveProducts() => _activeProductsCounter.Add(1);
    public void DecrementActiveProducts() => _activeProductsCounter.Add(-1);
}
