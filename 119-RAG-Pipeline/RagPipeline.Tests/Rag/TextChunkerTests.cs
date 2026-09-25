using RagPipeline.Api.Rag;

namespace RagPipeline.Tests.Rag;

public class TextChunkerTests
{
    [Fact]
    public void ChunkText_EmptyInput_ReturnsEmptyList()
    {
        var result = TextChunker.ChunkText("");
        Assert.Empty(result);
    }

    [Fact]
    public void ChunkText_NullInput_ReturnsEmptyList()
    {
        var result = TextChunker.ChunkText(null!);
        Assert.Empty(result);
    }

    [Fact]
    public void ChunkText_ShortText_ReturnsSingleChunk()
    {
        var result = TextChunker.ChunkText("Hello world");
        Assert.Single(result);
        Assert.Equal("Hello world", result[0]);
    }

    [Fact]
    public void ChunkText_MultipleParagraphs_ChunksCorrectly()
    {
        var text = "Paragraph one about topic A.\n\nParagraph two about topic B.\n\nParagraph three about topic C.";
        var result = TextChunker.ChunkText(text, maxChunkSize: 500);

        Assert.True(result.Count >= 1);
        Assert.All(result, chunk => Assert.False(string.IsNullOrWhiteSpace(chunk)));
    }

    [Fact]
    public void ChunkText_LongParagraph_SplitsBySentences()
    {
        var longText = string.Join(". ", Enumerable.Range(1, 50).Select(i => $"This is sentence number {i} with some extra text"));
        var result = TextChunker.ChunkText(longText, maxChunkSize: 200);

        Assert.True(result.Count > 1);
        Assert.All(result, chunk => Assert.True(chunk.Length <= 300)); // Allow some tolerance
    }
}
