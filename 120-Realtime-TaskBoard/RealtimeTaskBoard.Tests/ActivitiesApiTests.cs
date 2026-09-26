using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using RealtimeTaskBoard.Api.Features.Activities;
using RealtimeTaskBoard.Api.Features.Boards;

namespace RealtimeTaskBoard.Tests;

public class ActivitiesApiTests : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public ActivitiesApiTests(TestWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Activities_AreLogged_WhenBoardCreated()
    {
        var createResp = await _client.PostAsJsonAsync("/api/boards",
            new CreateBoardRequest("Activity Test", null));
        var board = (await createResp.Content.ReadFromJsonAsync<BoardDto>())!;

        var activities = await _client.GetFromJsonAsync<List<ActivityDto>>(
            $"/api/boards/{board.Id}/activities", JsonOptions);

        Assert.NotNull(activities);
        Assert.NotEmpty(activities);
        Assert.Contains(activities, a => a.ActionType == ActivityAction.Created && a.EntityType == "Board");
    }

    [Fact]
    public async Task Activities_RespectLimit()
    {
        var createResp = await _client.PostAsJsonAsync("/api/boards",
            new CreateBoardRequest("Limit Test", null));
        var board = (await createResp.Content.ReadFromJsonAsync<BoardDto>())!;

        var activities = await _client.GetFromJsonAsync<List<ActivityDto>>(
            $"/api/boards/{board.Id}/activities?limit=1", JsonOptions);

        Assert.NotNull(activities);
        Assert.Single(activities);
    }
}
