using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using InventoryIdServer.Api.Models;

namespace InventoryIdServer.Tests;

public class IdentityServerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public IdentityServerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task DiscoveryEndpoint_ReturnsValidConfiguration()
    {
        var response = await _client.GetAsync("/.well-known/openid-configuration");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.NotNull(doc);
        Assert.True(doc.RootElement.TryGetProperty("token_endpoint", out var tokenEndpoint));
        Assert.Contains("/connect/token", tokenEndpoint.GetString());
    }

    [Fact]
    public async Task TokenEndpoint_ValidClientCredentials_ReturnsAccessToken()
    {
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = "machine_worker",
            ["client_secret"] = "worker_secret_key_123",
            ["scope"] = "IdentityServerApi"
        });

        var response = await _client.PostAsync("/connect/token", form);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(token);
        Assert.False(string.IsNullOrWhiteSpace(token.access_token));
        Assert.Equal("Bearer", token.token_type, ignoreCase: true);
    }

    [Fact]
    public async Task TokenEndpoint_InvalidSecret_ReturnsBadRequest()
    {
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = "machine_worker",
            ["client_secret"] = "wrong_secret_abc",
            ["scope"] = "IdentityServerApi"
        });

        var response = await _client.PostAsync("/connect/token", form);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task InventoryApi_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/inventory");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task InventoryApi_WithValidToken_ReturnsInventoryList()
    {
        // 1. Obtain token
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = "machine_worker",
            ["client_secret"] = "worker_secret_key_123",
            ["scope"] = "IdentityServerApi"
        });

        var tokenRes = await _client.PostAsync("/connect/token", form);
        var token = await tokenRes.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(token);

        // 2. Call inventory API
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/inventory");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.access_token);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var items = await response.Content.ReadFromJsonAsync<List<InventoryItemDto>>();
        Assert.NotNull(items);
        Assert.NotEmpty(items);
    }

    [Fact]
    public async Task InventoryApi_PostItem_WithValidToken_ReturnsCreated()
    {
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = "machine_worker",
            ["client_secret"] = "worker_secret_key_123",
            ["scope"] = "IdentityServerApi"
        });

        var tokenRes = await _client.PostAsync("/connect/token", form);
        var token = await tokenRes.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(token);

        var newItem = new InventoryItemDto("SKU-TEST-NEW", "Test Item", 10, 49.99m);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/inventory");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.access_token);
        request.Content = JsonContent.Create(newItem);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<InventoryItemDto>();
        Assert.NotNull(created);
        Assert.Equal("SKU-TEST-NEW", created.Sku);
    }
}
