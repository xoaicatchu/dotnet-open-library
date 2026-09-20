using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using RedisPlatform.Api.Models;

namespace RedisPlatform.Tests;

public class RedisDemoTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public RedisDemoTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Counter_Increment_ReturnsIncrementedValue()
    {
        var key = $"counter_{Guid.NewGuid():N}";

        // First increment
        var res1 = await _client.PostAsync($"/api/redisdemo/counter/{key}?increment=5", null);
        Assert.Equal(HttpStatusCode.OK, res1.StatusCode);
        var data1 = await res1.Content.ReadFromJsonAsync<CounterResponseDto>();
        Assert.NotNull(data1);
        Assert.Equal(5, data1.Value);

        // Second increment
        var res2 = await _client.PostAsync($"/api/redisdemo/counter/{key}?increment=3", null);
        Assert.Equal(HttpStatusCode.OK, res2.StatusCode);
        var data2 = await res2.Content.ReadFromJsonAsync<CounterResponseDto>();
        Assert.NotNull(data2);
        Assert.Equal(8, data2.Value);
    }

    [Fact]
    public async Task Hash_SetAndGet_StoresAndRetrievesFields()
    {
        var key = $"hash_{Guid.NewGuid():N}";

        // Set field1
        var setRes1 = await _client.PostAsJsonAsync($"/api/redisdemo/hash/{key}", new HashFieldRequest("user_id", "42"));
        Assert.Equal(HttpStatusCode.OK, setRes1.StatusCode);

        // Set field2
        var setRes2 = await _client.PostAsJsonAsync($"/api/redisdemo/hash/{key}", new HashFieldRequest("role", "admin"));
        Assert.Equal(HttpStatusCode.OK, setRes2.StatusCode);

        // Get all
        var getRes = await _client.GetAsync($"/api/redisdemo/hash/{key}");
        Assert.Equal(HttpStatusCode.OK, getRes.StatusCode);
        var getData = await getRes.Content.ReadFromJsonAsync<HashResponseDto>();
        Assert.NotNull(getData);
        Assert.Equal(2, getData.Entries.Count);
        Assert.Equal("42", getData.Entries["user_id"]);
        Assert.Equal("admin", getData.Entries["role"]);
    }

    [Fact]
    public async Task Set_AddAndGet_StoresUniqueMembers()
    {
        var key = $"set_{Guid.NewGuid():N}";

        // Add members
        await _client.PostAsJsonAsync($"/api/redisdemo/set/{key}", new SetMemberRequest("tag-csharp"));
        await _client.PostAsJsonAsync($"/api/redisdemo/set/{key}", new SetMemberRequest("tag-dotnet"));
        await _client.PostAsJsonAsync($"/api/redisdemo/set/{key}", new SetMemberRequest("tag-csharp")); // Duplicate

        // Get members
        var getRes = await _client.GetAsync($"/api/redisdemo/set/{key}");
        Assert.Equal(HttpStatusCode.OK, getRes.StatusCode);
        var getData = await getRes.Content.ReadFromJsonAsync<SetResponseDto>();
        Assert.NotNull(getData);
        Assert.Equal(2, getData.Members.Count); // Set contains only unique items
        Assert.Contains("tag-csharp", getData.Members);
        Assert.Contains("tag-dotnet", getData.Members);
    }

    [Fact]
    public async Task Leaderboard_AddAndGetTop_ReturnsSortedEntries()
    {
        var key = $"leaderboard_{Guid.NewGuid():N}";

        // Add scores
        await _client.PostAsJsonAsync($"/api/redisdemo/leaderboard/{key}", new LeaderboardScoreRequest("PlayerAlpha", 1500.0));
        await _client.PostAsJsonAsync($"/api/redisdemo/leaderboard/{key}", new LeaderboardScoreRequest("PlayerBeta", 3200.0));
        await _client.PostAsJsonAsync($"/api/redisdemo/leaderboard/{key}", new LeaderboardScoreRequest("PlayerGamma", 2100.0));

        // Get top
        var getRes = await _client.GetAsync($"/api/redisdemo/leaderboard/{key}/top?take=3");
        Assert.Equal(HttpStatusCode.OK, getRes.StatusCode);
        var entries = await getRes.Content.ReadFromJsonAsync<List<LeaderboardEntryDto>>();
        Assert.NotNull(entries);
        Assert.Equal(3, entries.Count);

        // Should be ordered descending: PlayerBeta (3200) -> PlayerGamma (2100) -> PlayerAlpha (1500)
        Assert.Equal("PlayerBeta", entries[0].Member);
        Assert.Equal(3200.0, entries[0].Score);
        Assert.Equal(1, entries[0].Rank);

        Assert.Equal("PlayerGamma", entries[1].Member);
        Assert.Equal(2100.0, entries[1].Score);

        Assert.Equal("PlayerAlpha", entries[2].Member);
        Assert.Equal(1500.0, entries[2].Score);
    }

    [Fact]
    public async Task Publish_ReturnsSubscriberCount()
    {
        var request = new PublishMessageRequest("channel-alerts", "System reboot in 5 minutes");
        var response = await _client.PostAsJsonAsync("/api/redisdemo/publish", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var data = await response.Content.ReadFromJsonAsync<PublishResponseDto>();
        Assert.NotNull(data);
        Assert.Equal("channel-alerts", data.Channel);
        Assert.True(data.ReceiversCount >= 0);
    }

    [Fact]
    public async Task Counter_WithWhitespaceKey_ReturnsBadRequest()
    {
        var response = await _client.PostAsync("/api/redisdemo/counter/%20", null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Hash_WithEmptyField_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/redisdemo/hash/somekey", new HashFieldRequest("", "val"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
