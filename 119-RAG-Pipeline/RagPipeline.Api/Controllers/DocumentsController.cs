using Microsoft.AspNetCore.Mvc;
using RagPipeline.Api.Data;
using RagPipeline.Api.Models;
using RagPipeline.Api.Rag;

namespace RagPipeline.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController(DocumentStore store, IRagService ragService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Upload([FromBody] UploadDocumentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 200)
            return BadRequest("Invalid Title");
        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest("Content is required");

        var doc = new Document
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Content = request.Content,
            UploadedAt = DateTime.UtcNow
        };

        // Ingest into RAG pipeline (chunk + vectorize)
        var chunkCount = await ragService.IngestDocumentAsync(doc.Id, doc.Title, doc.Content);
        doc.ChunkCount = chunkCount;

        store.Add(doc);

        return CreatedAtAction(nameof(GetById), new { id = doc.Id }, doc);
    }

    [HttpGet]
    public IActionResult GetAll() => Ok(store.GetAll());

    [HttpGet("{id:guid}")]
    public IActionResult GetById(Guid id)
    {
        var doc = store.GetById(id);
        return doc is not null ? Ok(doc) : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var doc = store.GetById(id);
        if (doc is null) return NotFound();

        await ragService.RemoveDocumentAsync(id);
        store.Remove(id);

        return NoContent();
    }
}
