namespace AiAgentMessaging.Api.Models;

public class Order
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Product { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = "Submitted";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // AI enrichment fields - populated by IAiOrderEnricher, empty when AI disabled
    public string? AiCategory { get; set; }
    public string? AiSummary { get; set; }
    public string? AiProvider { get; set; }
}
