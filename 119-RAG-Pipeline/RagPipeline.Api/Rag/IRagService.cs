using RagPipeline.Api.Models;

namespace RagPipeline.Api.Rag;

/// <summary>
/// Abstraction for the RAG pipeline.
/// Implementations can use Semantic Kernel with real embeddings or a simple keyword-based fallback.
/// AI model is pluggable — workflow works with or without AI.
/// </summary>
public interface IRagService
{
    /// <summary>
    /// Ingest a document: chunk the text and store embeddings.
    /// </summary>
    Task<int> IngestDocumentAsync(Guid documentId, string title, string content, CancellationToken ct = default);

    /// <summary>
    /// Remove all chunks for a document.
    /// </summary>
    Task RemoveDocumentAsync(Guid documentId, CancellationToken ct = default);

    /// <summary>
    /// Ask a question: retrieve relevant chunks and generate an answer.
    /// </summary>
    Task<AskQuestionResponse> AskAsync(string question, int topK = 3, CancellationToken ct = default);

    /// <summary>
    /// Whether a real AI backend is available for generation.
    /// </summary>
    bool IsAiAvailable { get; }
}
