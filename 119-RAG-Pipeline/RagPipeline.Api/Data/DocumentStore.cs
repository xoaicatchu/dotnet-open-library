using System.Collections.Concurrent;
using RagPipeline.Api.Models;

namespace RagPipeline.Api.Data;

public class DocumentStore
{
    private readonly ConcurrentDictionary<Guid, Document> _documents = new();

    public void Add(Document doc) => _documents[doc.Id] = doc;

    public Document? GetById(Guid id)
    {
        _documents.TryGetValue(id, out var doc);
        return doc;
    }

    public IEnumerable<Document> GetAll() => _documents.Values;

    public bool Remove(Guid id) => _documents.TryRemove(id, out _);
}
