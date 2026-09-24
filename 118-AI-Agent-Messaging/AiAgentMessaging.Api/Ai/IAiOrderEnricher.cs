namespace AiAgentMessaging.Api.Ai;

/// <summary>
/// Abstraction for AI-based order enrichment.
/// Implementations can be swapped at runtime via configuration.
/// When AI is disabled, NoOpOrderEnricher provides safe defaults.
/// </summary>
public interface IAiOrderEnricher
{
    /// <summary>
    /// Enrich an order with AI-generated category and summary.
    /// </summary>
    Task<AiEnrichmentResult> EnrichAsync(OrderEnrichmentRequest request, CancellationToken ct = default);

    /// <summary>
    /// Whether this enricher has a functioning AI backend.
    /// </summary>
    bool IsAvailable { get; }
}
