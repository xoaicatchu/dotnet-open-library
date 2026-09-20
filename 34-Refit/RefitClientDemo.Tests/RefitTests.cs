using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using RefitClientDemo.Api.Models;
using RefitClientDemo.Api.Services;

namespace RefitClientDemo.Tests;

public class MockExternalHttpHandler : HttpMessageHandler
{
    private readonly List<UserProfile> _users = new()
    {
        new(1, "Alice Admin", "alice@example.com", "Admin", true, DateTime.UtcNow.AddDays(-10)),
        new(2, "Bob Developer", "bob@example.com", "Developer", true, DateTime.UtcNow.AddDays(-5)),
        new(3, "Charlie User", "charlie@example.com", "User", false, DateTime.UtcNow.AddDays(-1))
    };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var uri = request.RequestUri?.ToString() ?? string.Empty;
        var method = request.Method;

        HttpResponseMessage response;

        // GET /api/external/users or with query string
        if (method == HttpMethod.Get && uri.Contains("/api/external/users"))
        {
            if (request.RequestUri?.AbsolutePath == "/api/external/users")
            {
                var query = System.Web.HttpUtility.ParseQueryString(request.RequestUri.Query);
                var role = query["role"];
                var results = _users.AsEnumerable();
                if (!string.IsNullOrEmpty(role))
                {
                    results = results.Where(u => u.Role.Equals(role, StringComparison.OrdinalIgnoreCase));
                }

                response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(results.ToList())
                };
            }
            else
            {
                // GET /api/external/users/{id}
                var segments = request.RequestUri!.Segments;
                if (segments.Length > 0 && int.TryParse(segments[^1].TrimEnd('/'), out int id))
                {
                    var user = _users.FirstOrDefault(u => u.Id == id);
                    if (user != null)
                    {
                        response = new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = JsonContent.Create(user)
                        };
                    }
                    else
                    {
                        response = new HttpResponseMessage(HttpStatusCode.NotFound);
                    }
                }
                else
                {
                    response = new HttpResponseMessage(HttpStatusCode.NotFound);
                }
            }
        }
        // POST /api/external/users
        else if (method == HttpMethod.Post && uri.Contains("/api/external/users"))
        {
            var body = await request.Content!.ReadFromJsonAsync<CreateUserProfileRequest>(cancellationToken: cancellationToken);
            var newUser = new UserProfile(
                Id: 100,
                Name: body!.Name,
                Email: body.Email,
                Role: body.Role,
                IsActive: true,
                CreatedAt: DateTime.UtcNow
            );
            response = new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = JsonContent.Create(newUser)
            };
        }
        // PUT /api/external/users/{id}
        else if (method == HttpMethod.Put && uri.Contains("/api/external/users/"))
        {
            var segments = request.RequestUri!.Segments;
            if (segments.Length > 0 && int.TryParse(segments[^1].TrimEnd('/'), out int id))
            {
                if (id == 999)
                {
                    response = new HttpResponseMessage(HttpStatusCode.NotFound);
                }
                else
                {
                    var body = await request.Content!.ReadFromJsonAsync<UpdateUserProfileRequest>(cancellationToken: cancellationToken);
                    var updated = new UserProfile(
                        Id: id,
                        Name: body!.Name,
                        Email: body.Email,
                        Role: body.Role,
                        IsActive: body.IsActive,
                        CreatedAt: DateTime.UtcNow
                    );
                    response = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = JsonContent.Create(updated)
                    };
                }
            }
            else
            {
                response = new HttpResponseMessage(HttpStatusCode.NotFound);
            }
        }
        // DELETE /api/external/users/{id}
        else if (method == HttpMethod.Delete && uri.Contains("/api/external/users/"))
        {
            var segments = request.RequestUri!.Segments;
            if (segments.Length > 0 && int.TryParse(segments[^1].TrimEnd('/'), out int id))
            {
                if (id == 999)
                {
                    response = new HttpResponseMessage(HttpStatusCode.NotFound);
                }
                else
                {
                    response = new HttpResponseMessage(HttpStatusCode.NoContent);
                }
            }
            else
            {
                response = new HttpResponseMessage(HttpStatusCode.NotFound);
            }
        }
        else
        {
            response = new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        response.RequestMessage = request;
        return response;
    }
}

public class RefitTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public RefitTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddRefitClient<IUsersApiClient>()
                    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.example.com"))
                    .ConfigurePrimaryHttpMessageHandler(() => new MockExternalHttpHandler());
            });
        }).CreateClient();
    }

    [Fact]
    public async Task GetUsers_ReturnsListFromExternalApi()
    {
        var response = await _client.GetAsync("/api/externalusers");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var users = await response.Content.ReadFromJsonAsync<List<UserProfile>>();
        Assert.NotNull(users);
        Assert.Equal(3, users.Count);
    }

    [Fact]
    public async Task GetUsers_WithQueryFilter_PassesQueryToExternalApi()
    {
        var response = await _client.GetAsync("/api/externalusers?role=Admin");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var users = await response.Content.ReadFromJsonAsync<List<UserProfile>>();
        Assert.NotNull(users);
        Assert.Single(users);
        Assert.Equal("Alice Admin", users[0].Name);
    }

    [Fact]
    public async Task GetUserById_ExistingUser_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/externalusers/1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var user = await response.Content.ReadFromJsonAsync<UserProfile>();
        Assert.NotNull(user);
        Assert.Equal(1, user.Id);
        Assert.Equal("Alice Admin", user.Name);
    }

    [Fact]
    public async Task GetUserById_NonExistent_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/externalusers/999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_Valid_ReturnsCreated()
    {
        var request = new CreateUserProfileRequest("Neo Anderson", "neo@matrix.test", "ChosenOne");
        var response = await _client.PostAsJsonAsync("/api/externalusers", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<UserProfile>();
        Assert.NotNull(created);
        Assert.Equal(100, created.Id);
        Assert.Equal("Neo Anderson", created.Name);
    }

    [Fact]
    public async Task CreateUser_EmptyName_ReturnsBadRequest()
    {
        var request = new CreateUserProfileRequest("", "neo@matrix.test", "ChosenOne");
        var response = await _client.PostAsJsonAsync("/api/externalusers", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_Existing_ReturnsOk()
    {
        var request = new UpdateUserProfileRequest("Updated Name", "updated@test.com", "Senior", true);
        var response = await _client.PutAsJsonAsync("/api/externalusers/1", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await response.Content.ReadFromJsonAsync<UserProfile>();
        Assert.NotNull(updated);
        Assert.Equal("Updated Name", updated.Name);
    }

    [Fact]
    public async Task UpdateUser_NonExistent_ReturnsNotFound()
    {
        var request = new UpdateUserProfileRequest("Updated Name", "updated@test.com", "Senior", true);
        var response = await _client.PutAsJsonAsync("/api/externalusers/999", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_Existing_ReturnsNoContent()
    {
        var response = await _client.DeleteAsync("/api/externalusers/1");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_NonExistent_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync("/api/externalusers/999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
