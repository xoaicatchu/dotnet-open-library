namespace AiAgentMessaging.Api.Ai;

public record AiEnrichmentResult(
    string Category,
    string Summary,
    string Provider,
    bool IsFromAi);
