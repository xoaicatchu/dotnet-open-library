namespace AiAgentMessaging.Api.Ai;

public class AiProviderOptions
{
    public const string SectionName = "AiProvider";

    public bool Enabled { get; set; }
    public string Provider { get; set; } = "None";
    public string ModelId { get; set; } = "gpt-4o-mini";
    public string ApiKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
}
