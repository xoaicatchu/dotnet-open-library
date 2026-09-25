using System.Collections.Concurrent;
using RagPipeline.Api.Models;

namespace RagPipeline.Api.Rag;

/// <summary>
/// In-memory vector store using simple cosine similarity on TF-IDF-like keyword vectors.
/// No external dependencies — works without any AI model.
/// This is the fallback when AI/embeddings are disabled.
/// </summary>
public class InMemoryVectorStore
{
    private readonly ConcurrentDictionary<string, ChunkEntry> _chunks = new();

    public void AddChunk(Guid documentId, string documentTitle, int chunkIndex, string chunkText)
    {
        var key = $"{documentId}:{chunkIndex}";
        var vector = Vectorize(chunkText);
        _chunks[key] = new ChunkEntry(documentId, documentTitle, chunkIndex, chunkText, vector);
    }

    public void RemoveByDocumentId(Guid documentId)
    {
        var keysToRemove = _chunks.Keys.Where(k => k.StartsWith($"{documentId}:")).ToList();
        foreach (var key in keysToRemove)
            _chunks.TryRemove(key, out _);
    }

    public List<RetrievedChunk> Search(string query, int topK = 3)
    {
        if (_chunks.IsEmpty)
            return [];

        var queryVector = Vectorize(query);

        return _chunks.Values
            .Select(c => new
            {
                Chunk = c,
                Score = CosineSimilarity(queryVector, c.Vector)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .Select(x => new RetrievedChunk(
                x.Chunk.DocumentId,
                x.Chunk.DocumentTitle,
                x.Chunk.ChunkText,
                Math.Round(x.Score, 4)))
            .ToList();
    }

    public int Count => _chunks.Count;

    /// <summary>
    /// Simple term-frequency vectorization (bag of words).
    /// Not as good as real embeddings, but works without any AI model.
    /// </summary>
    private static Dictionary<string, double> Vectorize(string text)
    {
        var words = text
            .ToLowerInvariant()
            .Split([' ', '\t', '\n', '\r', '.', ',', '!', '?', ';', ':', '"', '\'', '(', ')', '[', ']', '{', '}'],
                StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 2) // Skip very short words
            .ToList();

        var tf = new Dictionary<string, double>();
        foreach (var word in words)
        {
            tf.TryGetValue(word, out var count);
            tf[word] = count + 1;
        }

        // Normalize
        var total = tf.Values.Sum();
        if (total > 0)
        {
            foreach (var key in tf.Keys.ToList())
                tf[key] /= total;
        }

        return tf;
    }

    private static double CosineSimilarity(Dictionary<string, double> a, Dictionary<string, double> b)
    {
        var dotProduct = 0.0;
        var normA = 0.0;
        var normB = 0.0;

        foreach (var (key, value) in a)
        {
            normA += value * value;
            if (b.TryGetValue(key, out var bValue))
                dotProduct += value * bValue;
        }

        foreach (var value in b.Values)
            normB += value * value;

        if (normA == 0 || normB == 0)
            return 0;

        return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }

    private record ChunkEntry(
        Guid DocumentId,
        string DocumentTitle,
        int ChunkIndex,
        string ChunkText,
        Dictionary<string, double> Vector);
}
