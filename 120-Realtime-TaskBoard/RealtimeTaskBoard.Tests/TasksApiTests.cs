using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using RealtimeTaskBoard.Api.Features.Boards;
using RealtimeTaskBoard.Api.Features.Columns;
using RealtimeTaskBoard.Api.Features.Tasks;

namespace RealtimeTaskBoard.Tests;

public class TasksApiTests : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public TasksApiTests(TestWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<(BoardDto Board, List<ColumnDto> Columns)> CreateBoardWithColumnsAsync()
    {
        var resp = await _client.PostAsJsonAsync("/api/boards", new CreateBoardRequest("Board", null));
        var board = (await resp.Content.ReadFromJsonAsync<BoardDto>())!;
        var columns = (await _client.GetFromJsonAsync<List<ColumnDto>>($"/api/boards/{board.Id}/columns"))!;
        return (board, columns);
    }

    [Fact]
    public async Task Create_AddsTaskToColumn()
    {
        var (_, columns) = await CreateBoardWithColumnsAsync();
        var backlogId = columns[0].Id;

        var request = new CreateTaskRequest("Test Task", "Description", TaskPriority.High, "dev@test.com", ["bug"], null);
        var resp = await _client.PostAsJsonAsync($"/api/columns/{backlogId}/tasks", request, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

        var task = await resp.Content.ReadFromJsonAsync<TaskDto>(JsonOptions);
        Assert.NotNull(task);
        Assert.Equal("Test Task", task.Title);
        Assert.Equal(TaskPriority.High, task.Priority);
        Assert.Contains("bug", task.Labels);
    }

    [Fact]
    public async Task GetByColumn_ReturnsTasks()
    {
        var (_, columns) = await CreateBoardWithColumnsAsync();
        var colId = columns[0].Id;

        await _client.PostAsJsonAsync($"/api/columns/{colId}/tasks",
            new CreateTaskRequest("Task 1", null, null, null, null, null));
        await _client.PostAsJsonAsync($"/api/columns/{colId}/tasks",
            new CreateTaskRequest("Task 2", null, null, null, null, null));

        var tasks = await _client.GetFromJsonAsync<List<TaskDto>>($"/api/columns/{colId}/tasks", JsonOptions);
        Assert.Equal(2, tasks!.Count);
        Assert.Equal(0, tasks[0].Position);
        Assert.Equal(1, tasks[1].Position);
    }

    [Fact]
    public async Task Update_ModifiesTask()
    {
        var (_, columns) = await CreateBoardWithColumnsAsync();
        var createResp = await _client.PostAsJsonAsync($"/api/columns/{columns[0].Id}/tasks",
            new CreateTaskRequest("Original", null, null, null, null, null));
        var created = (await createResp.Content.ReadFromJsonAsync<TaskDto>(JsonOptions))!;

        var updateResp = await _client.PutAsJsonAsync($"/api/tasks/{created.Id}",
            new UpdateTaskRequest("Updated", "New desc", TaskPriority.Critical, null, null, null), JsonOptions);
        updateResp.EnsureSuccessStatusCode();
        var updated = (await updateResp.Content.ReadFromJsonAsync<TaskDto>(JsonOptions))!;
        Assert.Equal("Updated", updated.Title);
        Assert.Equal(TaskPriority.Critical, updated.Priority);
    }

    [Fact]
    public async Task Move_MovesTaskBetweenColumns()
    {
        var (_, columns) = await CreateBoardWithColumnsAsync();
        var backlogId = columns[0].Id;
        var inProgressId = columns[1].Id;

        var createResp = await _client.PostAsJsonAsync($"/api/columns/{backlogId}/tasks",
            new CreateTaskRequest("Movable Task", null, null, null, null, null));
        var task = (await createResp.Content.ReadFromJsonAsync<TaskDto>(JsonOptions))!;

        var moveResp = await _client.PutAsJsonAsync($"/api/tasks/{task.Id}/move",
            new MoveTaskRequest(inProgressId, 0));
        moveResp.EnsureSuccessStatusCode();
        var moved = (await moveResp.Content.ReadFromJsonAsync<TaskDto>(JsonOptions))!;
        Assert.Equal(inProgressId, moved.ColumnId);
        Assert.Equal(0, moved.Position);
    }

    [Fact]
    public async Task Move_ReordersWithinSameColumn_ContiguousIndices()
    {
        var (_, columns) = await CreateBoardWithColumnsAsync();
        var colId = columns[0].Id;

        var tA = (await (await _client.PostAsJsonAsync($"/api/columns/{colId}/tasks",
            new CreateTaskRequest("Task A", null, null, null, null, null))).Content.ReadFromJsonAsync<TaskDto>(JsonOptions))!;
        var tB = (await (await _client.PostAsJsonAsync($"/api/columns/{colId}/tasks",
            new CreateTaskRequest("Task B", null, null, null, null, null))).Content.ReadFromJsonAsync<TaskDto>(JsonOptions))!;
        var tC = (await (await _client.PostAsJsonAsync($"/api/columns/{colId}/tasks",
            new CreateTaskRequest("Task C", null, null, null, null, null))).Content.ReadFromJsonAsync<TaskDto>(JsonOptions))!;

        // Move Task A (index 0) to index 2 (bottom)
        var moveResp = await _client.PutAsJsonAsync($"/api/tasks/{tA.Id}/move",
            new MoveTaskRequest(colId, 2));
        moveResp.EnsureSuccessStatusCode();

        var tasks = (await _client.GetFromJsonAsync<List<TaskDto>>($"/api/columns/{colId}/tasks", JsonOptions))!;
        Assert.Equal(3, tasks.Count);
        // Order should be B (0), C (1), A (2)
        Assert.Equal(tB.Id, tasks[0].Id);
        Assert.Equal(0, tasks[0].Position);
        Assert.Equal(tC.Id, tasks[1].Id);
        Assert.Equal(1, tasks[1].Position);
        Assert.Equal(tA.Id, tasks[2].Id);
        Assert.Equal(2, tasks[2].Position);
    }

    [Fact]
    public async Task Move_BetweenColumns_NormalizesBothColumnsContiguously()
    {
        var (_, columns) = await CreateBoardWithColumnsAsync();
        var col1 = columns[0].Id;
        var col2 = columns[1].Id;

        var t1 = (await (await _client.PostAsJsonAsync($"/api/columns/{col1}/tasks",
            new CreateTaskRequest("T1", null, null, null, null, null))).Content.ReadFromJsonAsync<TaskDto>(JsonOptions))!;
        var t2 = (await (await _client.PostAsJsonAsync($"/api/columns/{col1}/tasks",
            new CreateTaskRequest("T2", null, null, null, null, null))).Content.ReadFromJsonAsync<TaskDto>(JsonOptions))!;
        var t3 = (await (await _client.PostAsJsonAsync($"/api/columns/{col1}/tasks",
            new CreateTaskRequest("T3", null, null, null, null, null))).Content.ReadFromJsonAsync<TaskDto>(JsonOptions))!;

        var targetTask = (await (await _client.PostAsJsonAsync($"/api/columns/{col2}/tasks",
            new CreateTaskRequest("Target T", null, null, null, null, null))).Content.ReadFromJsonAsync<TaskDto>(JsonOptions))!;

        // Move T2 from col1 to col2 at position 0 (before Target T)
        var moveResp = await _client.PutAsJsonAsync($"/api/tasks/{t2.Id}/move",
            new MoveTaskRequest(col2, 0));
        moveResp.EnsureSuccessStatusCode();

        // Check col1: T1 (0), T3 (1)
        var col1Tasks = (await _client.GetFromJsonAsync<List<TaskDto>>($"/api/columns/{col1}/tasks", JsonOptions))!;
        Assert.Equal(2, col1Tasks.Count);
        Assert.Equal(t1.Id, col1Tasks[0].Id);
        Assert.Equal(0, col1Tasks[0].Position);
        Assert.Equal(t3.Id, col1Tasks[1].Id);
        Assert.Equal(1, col1Tasks[1].Position);

        // Check col2: T2 (0), Target T (1)
        var col2Tasks = (await _client.GetFromJsonAsync<List<TaskDto>>($"/api/columns/{col2}/tasks", JsonOptions))!;
        Assert.Equal(2, col2Tasks.Count);
        Assert.Equal(t2.Id, col2Tasks[0].Id);
        Assert.Equal(0, col2Tasks[0].Position);
        Assert.Equal(targetTask.Id, col2Tasks[1].Id);
        Assert.Equal(1, col2Tasks[1].Position);
    }

    [Fact]
    public async Task Delete_RemovesTask()
    {
        var (_, columns) = await CreateBoardWithColumnsAsync();
        var createResp = await _client.PostAsJsonAsync($"/api/columns/{columns[0].Id}/tasks",
            new CreateTaskRequest("ToDelete", null, null, null, null, null));
        var task = (await createResp.Content.ReadFromJsonAsync<TaskDto>(JsonOptions))!;

        var deleteResp = await _client.DeleteAsync($"/api/tasks/{task.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);
    }
}
