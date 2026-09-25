using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using RagPipeline.Api.Models;

namespace RagPipeline.Api.Rag;

/// <summary>
/// AI-powered RAG service using Semantic Kernel.
/// Uses real AI for answer generation but falls back to keyword search for retrieval
/// (since real embedding requires an embedding model endpoint).
/// Falls back gracefully if AI call fails — retrieval always works.
/// </summary>
public class SemanticKernelRagService : IRagService
{
    private readonly InMemoryVectorStore _vectorStore;
    private readonly Kernel _kernel;
    private readonly ILogger<SemanticKernelRagService> _logger;
    private readonly AiProviderOptions _options;

    public SemanticKernelRagService(
        InMemoryVectorStore vectorStore,
        Kernel kernel,
        ILogger<SemanticKernelRagService> logger,
        IOptions<AiProviderOptions> options)
    {
        _vectorStore = vectorStore;
        _kernel = kernel;
        _logger = logger;
        _options = options.Value;
    }

    public bool IsAiAvailable => true;

    public Task<int> IngestDocumentAsync(Guid documentId, string title, string content, CancellationToken ct = default)
    {
        var chunks = TextChunker.ChunkText(content);

        for (var i = 0; i < chunks.Count; i++)
        {
            _vectorStore.AddChunk(documentId, title, i, chunks[i]);
        }

        _logger.LogInformation(
            "Ingested document {DocumentId} ({Title}): {ChunkCount} chunks (AI mode)",
            documentId, title, chunks.Count);

        return Task.FromResult(chunks.Count);
    }

    public Task RemoveDocumentAsync(Guid documentId, CancellationToken ct = default)
    {
        _vectorStore.RemoveByDocumentId(documentId);
        _logger.LogInformation("Removed document {DocumentId} chunks", documentId);
        return Task.CompletedTask;
    }

    public async Task<AskQuestionResponse> AskAsync(string question, int topK = 3, CancellationToken ct = default)
    {
        // Step 1: Retrieve relevant chunks (keyword-based)
        var retrieved = _vectorStore.Search(question, topK);

        if (retrieved.Count == 0)
        {
            return new AskQuestionResponse(
                Answer: "No relevant documents found. Please upload documents first.",
                Sources: [],
                AiProvider: $"SemanticKernel/{_options.Provider}/{_options.ModelId}",
                IsFromAi: false);
        }

        // Step 2: Generate answer using AI with retrieved context
        try
        {
            var context = string.Join("\n\n", retrieved.Select(r =>
                $"[Source: {r.DocumentTitle}]\n{r.ChunkText}"));

            var prompt = $"""
                You are a helpful assistant. Answer the question based ONLY on the provided context.
                If the context doesn't contain enough information, say so honestly.

                Context:
                {context}

                Question: {question}

                Answer concisely and accurately:
                """;

            var result = await _kernel.InvokePromptAsync(prompt, cancellationToken: ct);
            var answer = result.GetValue<string>() ?? "Unable to generate answer.";

            _logger.LogInformation("AI-generated answer for '{Question}' using {Provider}", question, _options.Provider);

            return new AskQuestionResponse(
                Answer: answer,
                Sources: retrieved,
                AiProvider: $"SemanticKernel/{_options.Provider}/{_options.ModelId}",
                IsFromAi: true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI generation failed, falling back to raw chunks for '{Question}'", question);

            // Fallback: return raw chunks
            var fallbackAnswer = $"[AI unavailable] Based on {retrieved.Count} relevant document(s):\n\n" +
                                 string.Join("\n---\n", retrieved.Select(r => $"[{r.DocumentTitle}] {r.ChunkText}"));

            return new AskQuestionResponse(
                Answer: fallbackAnswer,
                Sources: retrieved,
                AiProvider: "SemanticKernel/Error",
                IsFromAi: false);
        }
    }
}
