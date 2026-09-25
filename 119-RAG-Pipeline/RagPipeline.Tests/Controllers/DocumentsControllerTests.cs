using System.Net;
using System.Net.Http.Json;
using RagPipeline.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace RagPipeline.Tests.Controllers;

public class DocumentsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public DocumentsControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Upload_ValidDocument_ReturnsCreated()
    {
        var request = new UploadDocumentRequest("Test Doc", "This is a test document with enough content to be meaningful.");
        var response = await _client.PostAsJsonAsync("/api/documents", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var doc = await response.Content.ReadFromJsonAsync<Document>();
        Assert.NotNull(doc);
        Assert.Equal("Test Doc", doc.Title);
        Assert.True(doc.ChunkCount > 0);
    }

    [Fact]
    public async Task Upload_EmptyTitle_ReturnsBadRequest()
    {
        var request = new UploadDocumentRequest("", "Some content");
        var response = await _client.PostAsJsonAsync("/api/documents", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_EmptyContent_ReturnsBadRequest()
    {
        var request = new UploadDocumentRequest("Title", "");
        var response = await _client.PostAsJsonAsync("/api/documents", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/documents");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ExistingDoc_ReturnsOk()
    {
        var request = new UploadDocumentRequest("For Get", "Content for retrieval test document.");
        var postResponse = await _client.PostAsJsonAsync("/api/documents", request);
        var doc = await postResponse.Content.ReadFromJsonAsync<Document>();

        var getResponse = await _client.GetAsync($"/api/documents/{doc!.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task GetById_NonExisting_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/documents/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ExistingDoc_ReturnsNoContent()
    {
        var request = new UploadDocumentRequest("For Delete", "Content to be deleted from the system.");
        var postResponse = await _client.PostAsJsonAsync("/api/documents", request);
        var doc = await postResponse.Content.ReadFromJsonAsync<Document>();

        var deleteResponse = await _client.DeleteAsync($"/api/documents/{doc!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/documents/{doc.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_NonExisting_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync($"/api/documents/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Swagger_IsAvailable()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        Assert.True(response.IsSuccessStatusCode);
    }
}
