using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using UserProfilesMapster.Api.Models;

namespace UserProfilesMapster.Tests;

public class UserMappingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public UserMappingTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_ReturnsMappedUserSummariesWithFlattenedFields()
    {
        var response = await _client.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var users = await response.Content.ReadFromJsonAsync<List<UserSummaryDto>>();
        Assert.NotNull(users);
        Assert.NotEmpty(users);

        var first = users.First();
        Assert.Equal("John Doe", first.FullName);
        Assert.Equal(2, first.RolesCount);
        Assert.Equal("Dark", first.Theme);
    }

    [Fact]
    public async Task GetById_ReturnsDetailedMappedUser()
    {
        var response = await _client.GetAsync("/api/users/1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var user = await response.Content.ReadFromJsonAsync<UserDetailDto>();
        Assert.NotNull(user);
        Assert.Equal(1, user.Id);
        Assert.Equal("johndoe", user.Username);
        Assert.Equal("John Doe", user.FullName);
        Assert.Equal("Dark", user.Preferences.Theme);
        Assert.Contains("Admin", user.Roles);
    }

    [Fact]
    public async Task Create_MapsRequestToUserAndPersists()
    {
        var request = new CreateUserRequest(
            Username: "brucewayne",
            Email: "bruce@waynecorp.com",
            FirstName: "Bruce",
            LastName: "Wayne",
            Bio: "CEO of Wayne Enterprises",
            Roles: new List<string> { "User", "VIP" },
            Theme: "Dark",
            Language: "en"
        );

        var response = await _client.PostAsJsonAsync("/api/users", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<UserDetailDto>();
        Assert.NotNull(created);
        Assert.True(created.Id > 0);
        Assert.Equal("Bruce Wayne", created.FullName);
        Assert.Equal("Dark", created.Preferences.Theme);
        Assert.Equal(2, created.Roles.Count);
    }

    [Fact]
    public async Task UpdateBio_MapsOntoExistingEntity()
    {
        var updateRequest = new UpdateUserBioRequest("Updated Senior Architect Bio", "Dark");
        var response = await _client.PutAsJsonAsync("/api/users/1/bio", updateRequest);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await response.Content.ReadFromJsonAsync<UserDetailDto>();
        Assert.NotNull(updated);
        Assert.Equal("Updated Senior Architect Bio", updated.Bio);
        Assert.Equal("Dark", updated.Preferences.Theme);
    }

    [Fact]
    public async Task GetById_NonExistent_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/users/9999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithEmptyUsername_ReturnsBadRequest()
    {
        var request = new CreateUserRequest("", "test@example.com", "Test", "User", "Bio", new List<string>());
        var response = await _client.PostAsJsonAsync("/api/users", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
