using System.Net;
using System.Net.Http.Json;
using RagPipeline.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace RagPipeline.Tests.Controllers;

public class QuestionsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public QuestionsControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Ask_EmptyQuestion_ReturnsBadRequest()
    {
        var request = new AskQuestionRequest("");
        var response = await _client.PostAsJsonAsync("/api/questions", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Ask_NoDocuments_ReturnsNoResults()
    {
        var request = new AskQuestionRequest("What is the onboarding process?");
        var response = await _client.PostAsJsonAsync("/api/questions", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AskQuestionResponse>();
        Assert.NotNull(result);
        Assert.False(result.IsFromAi);
    }

    [Fact]
    public async Task Ask_WithDocuments_ReturnsRelevantSources()
    {
        // Upload a document first
        var uploadRequest = new UploadDocumentRequest(
            "Company Policy",
            "Employees are entitled to 12 days of annual leave per year. Additional leave is granted for seniority.");
        await _client.PostAsJsonAsync("/api/documents", uploadRequest);

        // Ask a question
        var askRequest = new AskQuestionRequest("How many days of annual leave?");
        var response = await _client.PostAsJsonAsync("/api/questions", askRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AskQuestionResponse>();
        Assert.NotNull(result);
        Assert.NotEmpty(result.Sources);
        Assert.Contains("annual leave", result.Answer, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FullPipeline_UploadAskDelete_WorksEndToEnd()
    {
        // 1. Upload
        var doc = new UploadDocumentRequest(
            "RAG Test",
            "The RAG pipeline retrieves relevant documents and uses AI to generate answers based on context.");
        var uploadResponse = await _client.PostAsJsonAsync("/api/documents", doc);
        var created = await uploadResponse.Content.ReadFromJsonAsync<Document>();
        Assert.NotNull(created);

        // 2. Ask
        var askResponse = await _client.PostAsJsonAsync("/api/questions",
            new AskQuestionRequest("What does the RAG pipeline do?"));
        var answer = await askResponse.Content.ReadFromJsonAsync<AskQuestionResponse>();
        Assert.NotNull(answer);
        Assert.NotEmpty(answer.Sources);

        // 3. Delete
        var deleteResponse = await _client.DeleteAsync($"/api/documents/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }
}
