namespace RagPipeline.Api.Rag;

/// <summary>
/// Splits document text into smaller chunks for embedding.
/// Uses simple paragraph/sentence-based splitting.
/// </summary>
public static class TextChunker
{
    /// <summary>
    /// Split text into chunks of approximately maxChunkSize characters,
    /// respecting paragraph boundaries when possible.
    /// </summary>
    public static List<string> ChunkText(string text, int maxChunkSize = 500, int overlap = 50)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var chunks = new List<string>();
        var paragraphs = text.Split(["\r\n\r\n", "\n\n"], StringSplitOptions.RemoveEmptyEntries);

        var currentChunk = string.Empty;

        foreach (var para in paragraphs)
        {
            var trimmed = para.Trim();
            if (string.IsNullOrEmpty(trimmed))
                continue;

            if (currentChunk.Length + trimmed.Length + 1 <= maxChunkSize)
            {
                currentChunk = string.IsNullOrEmpty(currentChunk)
                    ? trimmed
                    : $"{currentChunk}\n{trimmed}";
            }
            else
            {
                if (!string.IsNullOrEmpty(currentChunk))
                    chunks.Add(currentChunk);

                // If single paragraph exceeds max, split by sentences
                if (trimmed.Length > maxChunkSize)
                {
                    var sentenceChunks = SplitBySentences(trimmed, maxChunkSize);
                    chunks.AddRange(sentenceChunks);
                    currentChunk = string.Empty;
                }
                else
                {
                    currentChunk = trimmed;
                }
            }
        }

        if (!string.IsNullOrEmpty(currentChunk))
            chunks.Add(currentChunk);

        // If no paragraphs found, treat entire text as one chunk or split by sentences
        if (chunks.Count == 0)
        {
            if (text.Trim().Length <= maxChunkSize)
                chunks.Add(text.Trim());
            else
                chunks.AddRange(SplitBySentences(text.Trim(), maxChunkSize));
        }

        return chunks;
    }

    private static List<string> SplitBySentences(string text, int maxChunkSize)
    {
        var chunks = new List<string>();
        var sentences = text.Split(['.', '!', '?'], StringSplitOptions.RemoveEmptyEntries);
        var current = string.Empty;

        foreach (var sentence in sentences)
        {
            var trimmed = sentence.Trim();
            if (string.IsNullOrEmpty(trimmed))
                continue;

            var withPeriod = $"{trimmed}.";

            if (current.Length + withPeriod.Length + 1 <= maxChunkSize)
            {
                current = string.IsNullOrEmpty(current)
                    ? withPeriod
                    : $"{current} {withPeriod}";
            }
            else
            {
                if (!string.IsNullOrEmpty(current))
                    chunks.Add(current);
                current = withPeriod;
            }
        }

        if (!string.IsNullOrEmpty(current))
            chunks.Add(current);

        return chunks;
    }
}
