using System.Net;
using System.Net.Http.Json;
using RealtimeTaskBoard.Api.Features.Boards;
using RealtimeTaskBoard.Api.Features.Columns;

namespace RealtimeTaskBoard.Tests;

public class ColumnsApiTests : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client;

    public ColumnsApiTests(TestWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<BoardDto> CreateBoardAsync()
    {
        var resp = await _client.PostAsJsonAsync("/api/boards", new CreateBoardRequest("Board", null));
        return (await resp.Content.ReadFromJsonAsync<BoardDto>())!;
    }

    [Fact]
    public async Task GetByBoard_ReturnsDefaultColumns()
    {
        var board = await CreateBoardAsync();
        var columns = await _client.GetFromJsonAsync<List<ColumnDto>>($"/api/boards/{board.Id}/columns");
        Assert.NotNull(columns);
        Assert.Equal(4, columns.Count);
    }

    [Fact]
    public async Task Create_AddsColumnToBoard()
    {
        var board = await CreateBoardAsync();
        var resp = await _client.PostAsJsonAsync($"/api/boards/{board.Id}/columns",
            new CreateColumnRequest("Custom Column", "#ff0000"));
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

        var columns = await _client.GetFromJsonAsync<List<ColumnDto>>($"/api/boards/{board.Id}/columns");
        Assert.Equal(5, columns!.Count);
        Assert.Equal("Custom Column", columns[4].Name);
        Assert.Equal(4, columns[4].Position); // 0-indexed, after 4 defaults
    }

    [Fact]
    public async Task Update_ModifiesColumnName()
    {
        var board = await CreateBoardAsync();
        var columns = await _client.GetFromJsonAsync<List<ColumnDto>>($"/api/boards/{board.Id}/columns");
        var firstCol = columns![0];

        var resp = await _client.PutAsJsonAsync($"/api/columns/{firstCol.Id}",
            new UpdateColumnRequest("Renamed", "#123456"));
        resp.EnsureSuccessStatusCode();
        var updated = await resp.Content.ReadFromJsonAsync<ColumnDto>();
        Assert.Equal("Renamed", updated!.Name);
    }

    [Fact]
    public async Task Delete_RemovesColumn()
    {
        var board = await CreateBoardAsync();
        var columns = await _client.GetFromJsonAsync<List<ColumnDto>>($"/api/boards/{board.Id}/columns");

        var deleteResp = await _client.DeleteAsync($"/api/columns/{columns![0].Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);

        var remaining = await _client.GetFromJsonAsync<List<ColumnDto>>($"/api/boards/{board.Id}/columns");
        Assert.Equal(3, remaining!.Count);
    }
}
