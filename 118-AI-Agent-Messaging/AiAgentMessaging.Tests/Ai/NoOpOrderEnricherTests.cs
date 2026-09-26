using AiAgentMessaging.Api.Ai;

namespace AiAgentMessaging.Tests.Ai;

public class NoOpOrderEnricherTests
{
    [Fact]
    public void IsAvailable_ReturnsFalse()
    {
        var enricher = new NoOpOrderEnricher();
        Assert.False(enricher.IsAvailable);
    }

    [Fact]
    public async Task EnrichAsync_ReturnsDefaultValues()
    {
        var enricher = new NoOpOrderEnricher();
        var request = new OrderEnrichmentRequest(Guid.NewGuid(), "Test", "Widget", 1, 10);

        var result = await enricher.EnrichAsync(request);

        Assert.Equal("Unclassified", result.Category);
        Assert.Equal("AI enrichment disabled", result.Summary);
        Assert.Equal("None", result.Provider);
        Assert.False(result.IsFromAi);
    }
}
