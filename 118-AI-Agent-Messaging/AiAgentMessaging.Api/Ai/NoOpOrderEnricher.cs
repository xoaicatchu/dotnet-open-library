namespace AiAgentMessaging.Api.Ai;

/// <summary>
/// Fallback enricher when AI is disabled.
/// Returns safe defaults so the business workflow continues unaffected.
/// </summary>
public class NoOpOrderEnricher : IAiOrderEnricher
{
    public bool IsAvailable => false;

    public Task<AiEnrichmentResult> EnrichAsync(OrderEnrichmentRequest request, CancellationToken ct = default)
    {
        return Task.FromResult(new AiEnrichmentResult(
            Category: "Unclassified",
            Summary: "AI enrichment disabled",
            Provider: "None",
            IsFromAi: false));
    }
}
