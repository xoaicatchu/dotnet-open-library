using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using AuthServerOpenIddict.Api.Models;

namespace AuthServerOpenIddict.Tests;

public class OpenIddictTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public OpenIddictTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task TokenEndpoint_ValidClientCredentials_ReturnsAccessToken()
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = "console-client",
            ["client_secret"] = "secret_key_987654321",
            ["scope"] = "api"
        });

        var response = await _client.PostAsync("/connect/token", content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var tokenResult = await response.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(tokenResult);
        Assert.False(string.IsNullOrWhiteSpace(tokenResult.access_token));
        Assert.Equal("Bearer", tokenResult.token_type, ignoreCase: true);
    }

    [Fact]
    public async Task TokenEndpoint_InvalidClientSecret_ReturnsUnauthorized()
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = "console-client",
            ["client_secret"] = "wrong_secret_123"
        });

        var response = await _client.PostAsync("/connect/token", content);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TokenEndpoint_UnsupportedGrantType_ReturnsBadRequest()
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = "console-client"
        });

        var response = await _client.PostAsync("/connect/token", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedResource_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/protectedresource/secret-data");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedResource_WithValidToken_ReturnsSecretData()
    {
        // 1. Obtain token
        var tokenContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = "console-client",
            ["client_secret"] = "secret_key_987654321",
            ["scope"] = "api"
        });

        var tokenRes = await _client.PostAsync("/connect/token", tokenContent);
        Assert.Equal(HttpStatusCode.OK, tokenRes.StatusCode);
        var token = await tokenRes.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(token);

        // 2. Call protected resource
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/protectedresource/secret-data");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.access_token);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<SecretDataResponse>();
        Assert.NotNull(result);
        Assert.Equal("console-client", result.ClientId);
        Assert.Contains("confidential", result.SecretMessage);
    }

    [Fact]
    public async Task UserInfo_WithValidToken_ReturnsCallerClaims()
    {
        var tokenContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = "console-client",
            ["client_secret"] = "secret_key_987654321",
            ["scope"] = "api"
        });

        var tokenRes = await _client.PostAsync("/connect/token", tokenContent);
        var token = await tokenRes.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(token);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/protectedresource/user-info");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.access_token);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var userInfo = await response.Content.ReadFromJsonAsync<UserInfoResponse>();
        Assert.NotNull(userInfo);
        Assert.Equal("console-client", userInfo.ClientId);
        Assert.Contains("ServiceWorker", userInfo.Roles);
    }
}
