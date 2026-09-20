using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using TaskBoard.Api.Models;
using Xunit;

namespace TaskBoard.Tests;

public class TaskModuleTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public TaskModuleTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    public record TaskDto(int Id, string Title, string? Description, bool IsCompleted, DateTime CreatedAt);

    [Fact]
    public async Task GetTasks_ReturnsTasks()
    {
        var response = await _client.GetAsync("/api/tasks");
        response.EnsureSuccessStatusCode();
        var tasks = await response.Content.ReadFromJsonAsync<List<TaskDto>>();
        Assert.NotNull(tasks);
        Assert.True(tasks.Count >= 2);
    }

    [Fact]
    public async Task GetTasks_FilterByCompletion_ReturnsCorrectTasks()
    {
        var response = await _client.GetAsync("/api/tasks?completed=true");
        response.EnsureSuccessStatusCode();
        var tasks = await response.Content.ReadFromJsonAsync<List<TaskDto>>();
        Assert.NotNull(tasks);
        Assert.All(tasks, t => Assert.True(t.IsCompleted));
    }

    [Fact]
    public async Task CrudRoundTrip_Works()
    {
        // 1. Create
        var createReq = new { Title = "Test Task", Description = "Desc" };
        var createRes = await _client.PostAsJsonAsync("/api/tasks", createReq);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        Assert.NotNull(createRes.Headers.Location);

        var createdTask = await createRes.Content.ReadFromJsonAsync<TaskDto>();
        Assert.NotNull(createdTask);
        Assert.Equal("Test Task", createdTask.Title);
        int taskId = createdTask.Id;

        // 2. Read
        var getRes = await _client.GetAsync($"/api/tasks/{taskId}");
        getRes.EnsureSuccessStatusCode();
        var fetchedTask = await getRes.Content.ReadFromJsonAsync<TaskDto>();
        Assert.Equal(taskId, fetchedTask!.Id);

        // 3. Update
        var updateReq = new { Title = "Updated Task", Description = "Updated Desc", IsCompleted = true };
        var updateRes = await _client.PutAsJsonAsync($"/api/tasks/{taskId}", updateReq);
        updateRes.EnsureSuccessStatusCode();
        
        var updatedTask = await updateRes.Content.ReadFromJsonAsync<TaskDto>();
        Assert.Equal("Updated Task", updatedTask!.Title);
        Assert.True(updatedTask.IsCompleted);

        // 4. Delete
        var deleteRes = await _client.DeleteAsync($"/api/tasks/{taskId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteRes.StatusCode);

        // 5. Verify Not Found
        var getDeletedRes = await _client.GetAsync($"/api/tasks/{taskId}");
        Assert.Equal(HttpStatusCode.NotFound, getDeletedRes.StatusCode);
    }

    [Fact]
    public async Task PatchComplete_MarksTaskAsCompleted()
    {
        var createReq = new { Title = "Patch Task" };
        var createRes = await _client.PostAsJsonAsync("/api/tasks", createReq);
        var createdTask = await createRes.Content.ReadFromJsonAsync<TaskDto>();
        int taskId = createdTask!.Id;

        var patchRes = await _client.PatchAsync($"/api/tasks/{taskId}/complete", null);
        patchRes.EnsureSuccessStatusCode();

        var patchedTask = await patchRes.Content.ReadFromJsonAsync<TaskDto>();
        Assert.True(patchedTask!.IsCompleted);
    }

    [Fact]
    public async Task CreateTask_WithEmptyTitle_Returns400()
    {
        var createReq = new { Title = "", Description = "Desc" };
        var createRes = await _client.PostAsJsonAsync("/api/tasks", createReq);
        Assert.Equal(HttpStatusCode.BadRequest, createRes.StatusCode);
    }

    [Fact]
    public async Task GetTask_WithNonExistentId_Returns404()
    {
        var response = await _client.GetAsync("/api/tasks/9999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Swagger_IsAvailableInDevelopment()
    {
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
        });
        var client = factory.CreateClient();
        
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();
    }
}
