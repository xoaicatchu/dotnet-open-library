using RagPipeline.Api.Models;

namespace RagPipeline.Api.Rag;

/// <summary>
/// Keyword-based RAG service that works without any AI model.
/// Uses TF-IDF cosine similarity for retrieval and returns raw chunks as "answer".
/// This is the fallback when AI is disabled — the pipeline still works end-to-end.
/// </summary>
public class KeywordRagService(InMemoryVectorStore vectorStore, ILogger<KeywordRagService> logger)
    : IRagService
{
    public bool IsAiAvailable => false;

    public Task<int> IngestDocumentAsync(Guid documentId, string title, string content, CancellationToken ct = default)
    {
        var chunks = TextChunker.ChunkText(content);

        for (var i = 0; i < chunks.Count; i++)
        {
            vectorStore.AddChunk(documentId, title, i, chunks[i]);
        }

        logger.LogInformation(
            "Ingested document {DocumentId} ({Title}): {ChunkCount} chunks (keyword mode)",
            documentId, title, chunks.Count);

        return Task.FromResult(chunks.Count);
    }

    public Task RemoveDocumentAsync(Guid documentId, CancellationToken ct = default)
    {
        vectorStore.RemoveByDocumentId(documentId);
        logger.LogInformation("Removed document {DocumentId} chunks", documentId);
        return Task.CompletedTask;
    }

    public Task<AskQuestionResponse> AskAsync(string question, int topK = 3, CancellationToken ct = default)
    {
        var retrieved = vectorStore.Search(question, topK);

        // Without AI, concatenate the most relevant chunks as the "answer"
        var answer = retrieved.Count > 0
            ? $"Based on {retrieved.Count} relevant document(s):\n\n" +
              string.Join("\n---\n", retrieved.Select(r => $"[{r.DocumentTitle}] {r.ChunkText}"))
            : "No relevant documents found. Please upload documents first.";

        logger.LogInformation("Keyword search for '{Question}': {ResultCount} results", question, retrieved.Count);

        return Task.FromResult(new AskQuestionResponse(
            Answer: answer,
            Sources: retrieved,
            AiProvider: "None (keyword search)",
            IsFromAi: false));
    }
}
