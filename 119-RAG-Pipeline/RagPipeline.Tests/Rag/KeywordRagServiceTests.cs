using Microsoft.Extensions.Logging;
using RagPipeline.Api.Rag;

namespace RagPipeline.Tests.Rag;

public class KeywordRagServiceTests
{
    private readonly InMemoryVectorStore _vectorStore = new();
    private readonly KeywordRagService _service;

    public KeywordRagServiceTests()
    {
        var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<KeywordRagService>();
        _service = new KeywordRagService(_vectorStore, logger);
    }

    [Fact]
    public void IsAiAvailable_ReturnsFalse()
    {
        Assert.False(_service.IsAiAvailable);
    }

    [Fact]
    public async Task IngestDocument_CreatesChunks()
    {
        var docId = Guid.NewGuid();
        var count = await _service.IngestDocumentAsync(docId, "Test", "Some test content here.");
        Assert.True(count > 0);
        Assert.True(_vectorStore.Count > 0);
    }

    [Fact]
    public async Task AskAsync_WithDocuments_ReturnsRelevantAnswer()
    {
        await _service.IngestDocumentAsync(Guid.NewGuid(), "Policy",
            "Employees get 12 days of annual leave. Leave increases by 1 day for every 5 years of service.");

        var response = await _service.AskAsync("How many days of annual leave?");

        Assert.NotNull(response);
        Assert.False(response.IsFromAi);
        Assert.Equal("None (keyword search)", response.AiProvider);
        Assert.NotEmpty(response.Sources);
    }

    [Fact]
    public async Task AskAsync_NoDocuments_ReturnsNoResults()
    {
        var response = await _service.AskAsync("random question");

        Assert.Contains("No relevant documents found", response.Answer);
        Assert.Empty(response.Sources);
    }

    [Fact]
    public async Task RemoveDocument_RemovesFromStore()
    {
        var docId = Guid.NewGuid();
        await _service.IngestDocumentAsync(docId, "Temp", "Temporary content for testing");
        Assert.True(_vectorStore.Count > 0);

        await _service.RemoveDocumentAsync(docId);
        Assert.Equal(0, _vectorStore.Count);
    }
}
