using RagPipeline.Api.Rag;

namespace RagPipeline.Tests.Rag;

public class InMemoryVectorStoreTests
{
    [Fact]
    public void AddChunk_IncreasesCount()
    {
        var store = new InMemoryVectorStore();
        store.AddChunk(Guid.NewGuid(), "Doc1", 0, "Hello world this is a test document");
        Assert.Equal(1, store.Count);
    }

    [Fact]
    public void Search_RelevantQuery_ReturnsResults()
    {
        var store = new InMemoryVectorStore();
        var docId = Guid.NewGuid();
        store.AddChunk(docId, "Tech Doc", 0, "Machine learning and artificial intelligence are transforming industries");
        store.AddChunk(docId, "Tech Doc", 1, "Cloud computing enables scalable infrastructure deployment");
        store.AddChunk(Guid.NewGuid(), "Food Doc", 0, "The recipe requires flour, sugar, and butter for baking");

        var results = store.Search("artificial intelligence machine learning", topK: 2);

        Assert.NotEmpty(results);
        Assert.Equal("Tech Doc", results[0].DocumentTitle);
        Assert.True(results[0].RelevanceScore > 0);
    }

    [Fact]
    public void Search_EmptyStore_ReturnsEmpty()
    {
        var store = new InMemoryVectorStore();
        var results = store.Search("test query");
        Assert.Empty(results);
    }

    [Fact]
    public void RemoveByDocumentId_RemovesCorrectChunks()
    {
        var store = new InMemoryVectorStore();
        var docId1 = Guid.NewGuid();
        var docId2 = Guid.NewGuid();

        store.AddChunk(docId1, "Doc1", 0, "First document chunk one");
        store.AddChunk(docId1, "Doc1", 1, "First document chunk two");
        store.AddChunk(docId2, "Doc2", 0, "Second document chunk one");

        Assert.Equal(3, store.Count);

        store.RemoveByDocumentId(docId1);

        Assert.Equal(1, store.Count);
        var results = store.Search("second document");
        Assert.Single(results);
        Assert.Equal("Doc2", results[0].DocumentTitle);
    }

    [Fact]
    public void Search_UnrelatedQuery_ReturnsLowOrNoResults()
    {
        var store = new InMemoryVectorStore();
        store.AddChunk(Guid.NewGuid(), "Doc", 0, "The quick brown fox jumps over the lazy dog");

        var results = store.Search("quantum physics superconductor");
        // May return empty or very low score
        Assert.True(results.Count == 0 || results[0].RelevanceScore < 0.1);
    }
}
