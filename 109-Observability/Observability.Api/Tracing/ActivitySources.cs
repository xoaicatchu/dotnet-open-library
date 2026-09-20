using System.Diagnostics;

namespace Observability.Api.Tracing;

public static class ActivitySources
{
    public static readonly ActivitySource Api = new("Observability.Api", "1.0.0");
}
