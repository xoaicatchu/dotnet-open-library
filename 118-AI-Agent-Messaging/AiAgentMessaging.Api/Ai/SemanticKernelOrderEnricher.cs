using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace AiAgentMessaging.Api.Ai;

/// <summary>
/// AI enricher using Microsoft Semantic Kernel.
/// Supports OpenAI, Azure OpenAI, and any OpenAI-compatible endpoint (Ollama).
/// Falls back gracefully if the AI call fails.
/// </summary>
public class SemanticKernelOrderEnricher : IAiOrderEnricher
{
    private readonly Kernel _kernel;
    private readonly ILogger<SemanticKernelOrderEnricher> _logger;
    private readonly AiProviderOptions _options;

    public SemanticKernelOrderEnricher(
        Kernel kernel,
        ILogger<SemanticKernelOrderEnricher> logger,
        IOptions<AiProviderOptions> options)
    {
        _kernel = kernel;
        _logger = logger;
        _options = options.Value;
    }

    public bool IsAvailable => true;

    public async Task<AiEnrichmentResult> EnrichAsync(OrderEnrichmentRequest request, CancellationToken ct = default)
    {
        try
        {
            var prompt = $"""
Analyze this order and provide:
1. Category (one of: Electronics, Clothing, Food, Home, Sports, Other)
2. Brief summary (max 50 words)

Order: Customer={request.CustomerName}, Product={request.Product}, Qty={request.Quantity}, Total=${request.TotalPrice}

Respond in format:
Category: <category>
Summary: <summary>
""";

            var result = await _kernel.InvokePromptAsync(prompt, cancellationToken: ct);
            var text = result.GetValue<string>() ?? string.Empty;

            var category = ExtractField(text, "Category") ?? "Other";
            var summary = ExtractField(text, "Summary") ?? "AI analysis completed";

            _logger.LogInformation("AI enriched order {OrderId}: Category={Category}", request.OrderId, category);

            return new AiEnrichmentResult(
                Category: category,
                Summary: summary,
                Provider: $"SemanticKernel/{_options.Provider}/{_options.ModelId}",
                IsFromAi: true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI enrichment failed for order {OrderId}, falling back to defaults", request.OrderId);

            return new AiEnrichmentResult(
                Category: "Unclassified",
                Summary: $"AI enrichment failed: {ex.Message}",
                Provider: "SemanticKernel/Error",
                IsFromAi: false);
        }
    }

    private static string? ExtractField(string text, string field)
    {
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            if (line.StartsWith($"{field}:", StringComparison.OrdinalIgnoreCase))
            {
                return line[$"{field}:".Length..].Trim();
            }
        }
        return null;
    }
}
