using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using NswagToolchain.Api.Models;
using Xunit;
using TaskStatus = NswagToolchain.Api.Models.TaskStatus;

namespace NswagToolchain.Tests;

public class NswagIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public NswagIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetOpenApiSpec_Returns200WithJsonSpec()
    {
        // Act
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("Project Tasks API", json);
        Assert.Contains("/api/tasks", json);
    }

    [Fact]
    public async Task GetSwaggerUi_Returns200()
    {
        // Act
        var response = await _client.GetAsync("/swagger/index.html");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetReDocUi_Returns200()
    {
        // Act
        var response = await _client.GetAsync("/redoc/index.html");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GenerateCSharpClient_Returns200WithCode()
    {
        // Act
        var response = await _client.GetAsync("/api/client-gen/csharp");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<GeneratedClientResponse>();
        Assert.NotNull(result);
        Assert.Equal("CSharp", result.Language);
        Assert.Contains("TasksClient", result.Code);
        Assert.Contains("ProjectManagement.Client", result.Code);
    }

    [Fact]
    public async Task GenerateTypeScriptClient_Returns200WithCode()
    {
        // Act
        var response = await _client.GetAsync("/api/client-gen/typescript");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<GeneratedClientResponse>();
        Assert.NotNull(result);
        Assert.Equal("TypeScript", result.Language);
        Assert.Contains("TasksClient", result.Code);
    }

    [Fact]
    public async Task GetTasks_Returns200WithList()
    {
        // Act
        var response = await _client.GetAsync("/api/tasks");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tasks = await response.Content.ReadFromJsonAsync<List<ProjectTask>>();
        Assert.NotNull(tasks);
        Assert.True(tasks.Count >= 2);
    }

    [Fact]
    public async Task GetTasks_WithFilter_ReturnsMatchingTasks()
    {
        // Act
        var response = await _client.GetAsync("/api/tasks?status=InProgress");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tasks = await response.Content.ReadFromJsonAsync<List<ProjectTask>>();
        Assert.NotNull(tasks);
        Assert.All(tasks, t => Assert.Equal(TaskStatus.InProgress, t.Status));
    }

    [Fact]
    public async Task GetTaskById_NotFound_Returns404()
    {
        // Act
        var response = await _client.GetAsync($"/api/tasks/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateTask_Valid_Returns201WithLocation()
    {
        // Arrange
        var request = new CreateTaskRequest("New Feature Task", "Description of feature", TaskPriority.High, "dev@test.com");

        // Act
        var response = await _client.PostAsJsonAsync("/api/tasks", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var created = await response.Content.ReadFromJsonAsync<ProjectTask>();
        Assert.NotNull(created);
        Assert.Equal("New Feature Task", created.Title);
        Assert.Equal(TaskStatus.Backlog, created.Status);
    }

    [Fact]
    public async Task UpdateTaskStatus_Valid_Returns200()
    {
        // Arrange
        var createRes = await _client.PostAsJsonAsync("/api/tasks", new CreateTaskRequest("Task to update", "Desc"));
        var created = await createRes.Content.ReadFromJsonAsync<ProjectTask>();
        Assert.NotNull(created);

        // Act
        var response = await _client.PutAsJsonAsync($"/api/tasks/{created.Id}/status", new UpdateTaskStatusRequest(TaskStatus.Done));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<ProjectTask>();
        Assert.NotNull(updated);
        Assert.Equal(TaskStatus.Done, updated.Status);
    }

    [Fact]
    public async Task DeleteTask_Valid_Returns204()
    {
        // Arrange
        var createRes = await _client.PostAsJsonAsync("/api/tasks", new CreateTaskRequest("Task to delete", "Desc"));
        var created = await createRes.Content.ReadFromJsonAsync<ProjectTask>();
        Assert.NotNull(created);

        // Act
        var response = await _client.DeleteAsync($"/api/tasks/{created.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify it's gone
        var getRes = await _client.GetAsync($"/api/tasks/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getRes.StatusCode);
    }
}
