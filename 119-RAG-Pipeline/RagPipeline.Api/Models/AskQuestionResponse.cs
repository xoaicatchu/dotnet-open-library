namespace RagPipeline.Api.Models;

public record AskQuestionResponse(
    string Answer,
    List<RetrievedChunk> Sources,
    string AiProvider,
    bool IsFromAi);

public record RetrievedChunk(
    Guid DocumentId,
    string DocumentTitle,
    string ChunkText,
    double RelevanceScore);
