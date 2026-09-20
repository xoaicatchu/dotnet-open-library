using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AuthStack.Api.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using AuthStack.Api.Data;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;

namespace AuthStack.Tests;

public class AuthIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public AuthIntegrationTests(WebApplicationFactory<Program> factory)
    {
        var dbName = Guid.NewGuid().ToString() + ".db";
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AuthDbContext>));
                if (descriptor != null) services.Remove(descriptor);
                
                services.AddDbContext<AuthDbContext>(options =>
                {
                    options.UseSqlite($"Data Source={dbName}");
                });
            });
        });

        // Ensure database is created and clear it
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
        }

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Register_Valid_Returns201()
    {
        var req = new RegisterRequest("testuser1", "test1@test.com", "Password123!");
        var res = await _client.PostAsJsonAsync("/api/auth/register", req);
        res.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Register_Duplicate_Returns409()
    {
        var req = new RegisterRequest("testuser2", "test2@test.com", "Password123!");
        await _client.PostAsJsonAsync("/api/auth/register", req);
        var res2 = await _client.PostAsJsonAsync("/api/auth/register", req);
        res2.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_Valid_Returns200AndTokens()
    {
        var req = new RegisterRequest("testuser3", "test3@test.com", "Password123!");
        await _client.PostAsJsonAsync("/api/auth/register", req);

        var loginReq = new LoginRequest("testuser3", "Password123!");
        var res = await _client.PostAsJsonAsync("/api/auth/login", loginReq);
        
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokens = await res.Content.ReadFromJsonAsync<TokenResponse>();
        tokens.Should().NotBeNull();
        tokens!.AccessToken.Should().NotBeNullOrEmpty();
        tokens.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var req = new RegisterRequest("testuser4", "test4@test.com", "Password123!");
        await _client.PostAsJsonAsync("/api/auth/register", req);

        var loginReq = new LoginRequest("testuser4", "WrongPassword!");
        var res = await _client.PostAsJsonAsync("/api/auth/login", loginReq);
        
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMe_NoToken_Returns401()
    {
        var res = await _client.GetAsync("/api/users/me");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMe_ValidToken_Returns200()
    {
        var req = new RegisterRequest("testuser5", "test5@test.com", "Password123!");
        await _client.PostAsJsonAsync("/api/auth/register", req);
        var loginReq = new LoginRequest("testuser5", "Password123!");
        var loginRes = await _client.PostAsJsonAsync("/api/auth/login", loginReq);
        var tokens = await loginRes.Content.ReadFromJsonAsync<TokenResponse>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
        var res = await _client.GetAsync("/api/users/me");
        
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AdminDashboard_UserRole_Returns403()
    {
        var req = new RegisterRequest("testuser6", "test6@test.com", "Password123!");
        await _client.PostAsJsonAsync("/api/auth/register", req);
        var loginReq = new LoginRequest("testuser6", "Password123!");
        var loginRes = await _client.PostAsJsonAsync("/api/auth/login", loginReq);
        var tokens = await loginRes.Content.ReadFromJsonAsync<TokenResponse>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
        var res = await _client.GetAsync("/api/admin/dashboard");
        
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminDashboard_AdminRole_Returns200()
    {
        var req = new RegisterRequest("adminuser", "admin@test.com", "Password123!", "Admin");
        await _client.PostAsJsonAsync("/api/auth/register", req);
        var loginReq = new LoginRequest("adminuser", "Password123!");
        var loginRes = await _client.PostAsJsonAsync("/api/auth/login", loginReq);
        var tokens = await loginRes.Content.ReadFromJsonAsync<TokenResponse>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
        var res = await _client.GetAsync("/api/admin/dashboard");
        
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Refresh_ValidToken_Returns200AndNewTokens()
    {
        var req = new RegisterRequest("testuser7", "test7@test.com", "Password123!");
        await _client.PostAsJsonAsync("/api/auth/register", req);
        var loginReq = new LoginRequest("testuser7", "Password123!");
        var loginRes = await _client.PostAsJsonAsync("/api/auth/login", loginReq);
        var tokens = await loginRes.Content.ReadFromJsonAsync<TokenResponse>();

        var refreshReq = new RefreshTokenRequest(tokens!.RefreshToken);
        var res = await _client.PostAsJsonAsync("/api/auth/refresh", refreshReq);
        
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var newTokens = await res.Content.ReadFromJsonAsync<TokenResponse>();
        newTokens.Should().NotBeNull();
        newTokens!.AccessToken.Should().NotBeNullOrEmpty();
        newTokens.RefreshToken.Should().NotBeNullOrEmpty();
        newTokens.RefreshToken.Should().NotBe(tokens.RefreshToken);
    }

    [Fact]
    public async Task Refresh_InvalidToken_Returns401()
    {
        var refreshReq = new RefreshTokenRequest("invalid-token");
        var res = await _client.PostAsJsonAsync("/api/auth/refresh", refreshReq);
        
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Revoke_WithToken_Returns200()
    {
        var req = new RegisterRequest("testuser8", "test8@test.com", "Password123!");
        await _client.PostAsJsonAsync("/api/auth/register", req);
        var loginReq = new LoginRequest("testuser8", "Password123!");
        var loginRes = await _client.PostAsJsonAsync("/api/auth/login", loginReq);
        var tokens = await loginRes.Content.ReadFromJsonAsync<TokenResponse>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
        var revokeReq = new RevokeRequest("testuser8");
        var res = await _client.PostAsJsonAsync("/api/auth/revoke", revokeReq);
        
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Refresh_AfterRevoke_Returns401()
    {
        var req = new RegisterRequest("testuser9", "test9@test.com", "Password123!");
        await _client.PostAsJsonAsync("/api/auth/register", req);
        var loginReq = new LoginRequest("testuser9", "Password123!");
        var loginRes = await _client.PostAsJsonAsync("/api/auth/login", loginReq);
        var tokens = await loginRes.Content.ReadFromJsonAsync<TokenResponse>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
        var revokeReq = new RevokeRequest("testuser9");
        await _client.PostAsJsonAsync("/api/auth/revoke", revokeReq);

        var refreshReq = new RefreshTokenRequest(tokens!.RefreshToken);
        var res = await _client.PostAsJsonAsync("/api/auth/refresh", refreshReq);
        
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
