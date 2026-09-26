using System.Net;
using System.Net.Http.Json;
using RealtimeTaskBoard.Api.Features.Boards;

namespace RealtimeTaskBoard.Tests;

public class BoardsApiTests : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client;

    public BoardsApiTests(TestWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_ReturnsEmptyList_WhenNoBoards()
    {
        var response = await _client.GetAsync("/api/boards");
        response.EnsureSuccessStatusCode();
        var boards = await response.Content.ReadFromJsonAsync<List<BoardDto>>();
        Assert.NotNull(boards);
    }

    [Fact]
    public async Task Create_ReturnsCreatedBoard_WithDefaultColumns()
    {
        var request = new CreateBoardRequest("Test Board", "Test Description");
        var response = await _client.PostAsJsonAsync("/api/boards", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var board = await response.Content.ReadFromJsonAsync<BoardDto>();
        Assert.NotNull(board);
        Assert.Equal("Test Board", board.Name);

        // Verify board has 4 default columns
        var detail = await _client.GetFromJsonAsync<BoardDetailDto>($"/api/boards/{board.Id}");
        Assert.NotNull(detail);
        Assert.Equal(4, detail.Columns.Count);
        Assert.Equal("Backlog", detail.Columns[0].Name);
        Assert.Equal("Done", detail.Columns[3].Name);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenBoardDoesNotExist()
    {
        var response = await _client.GetAsync($"/api/boards/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_ModifiesBoardName()
    {
        // Create
        var createResp = await _client.PostAsJsonAsync("/api/boards", new CreateBoardRequest("Original", null));
        var created = await createResp.Content.ReadFromJsonAsync<BoardDto>();

        // Update
        var updateResp = await _client.PutAsJsonAsync($"/api/boards/{created!.Id}", new UpdateBoardRequest("Updated", "New desc"));
        updateResp.EnsureSuccessStatusCode();
        var updated = await updateResp.Content.ReadFromJsonAsync<BoardDto>();
        Assert.Equal("Updated", updated!.Name);
    }

    [Fact]
    public async Task Delete_RemovesBoard()
    {
        var createResp = await _client.PostAsJsonAsync("/api/boards", new CreateBoardRequest("ToDelete", null));
        var created = await createResp.Content.ReadFromJsonAsync<BoardDto>();

        var deleteResp = await _client.DeleteAsync($"/api/boards/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);

        var getResp = await _client.GetAsync($"/api/boards/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResp.StatusCode);
    }
}
