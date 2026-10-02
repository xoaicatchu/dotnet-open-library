using System.Net;
using System.Net.Http.Json;
using StockFlow.Tests.Infrastructure;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StockFlow.Api.Data;

namespace StockFlow.Tests;

[Collection("Postgres")]
public sealed class AuthTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Anonymous_me_returns_401()
    {
        await using var factory = new StockFlowApiFactory(postgres.Database.GetConnectionString());
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().GetAsync("/api/auth/me")).StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("wrong-token")]
    public async Task Login_requires_valid_csrf(string? token)
    {
        await using var factory = new StockFlowApiFactory(postgres.Database.GetConnectionString());
        var client = factory.CreateClient();
        if (token is not null) client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", token);
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "nobody@example.com", password = "irrelevant" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Csrf_is_returned_as_json()
    {
        await using var factory = new StockFlowApiFactory(postgres.Database.GetConnectionString());
        var response = await factory.CreateClient().GetAsync("/api/auth/csrf");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("requestToken").GetString()));
    }

    internal static async Task RefreshCsrfAsync(HttpClient client)
    {
        var csrf = await client.GetFromJsonAsync<JsonElement>("/api/auth/csrf");
        client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", csrf.GetProperty("requestToken").GetString());
    }

    [Fact]
    public async Task Login_logout_rotate_identity_bound_csrf_and_cookie_is_shared_by_hosts()
    {
        await using var first = new StockFlowApiFactory(postgres.Database.GetConnectionString());
        var email = $"admin-{Guid.NewGuid():N}@example.com";
        var password = $"Valid!{Guid.NewGuid():N}";
        await first.CreateUserAsync(email, password, "Admin");
        var client = first.CreateClient();
        await RefreshCsrfAsync(client);
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.NoContent, login.StatusCode);
        var cookie = login.Headers.GetValues("Set-Cookie").Single(x => x.StartsWith("StockFlow.Session=")).Split(';')[0];
        await using var second = new StockFlowApiFactory(postgres.Database.GetConnectionString());
        var other = second.CreateClient();
        other.DefaultRequestHeaders.Add("Cookie", cookie);
        var me = await other.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.DoesNotContain("passwordHash", await me.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        // The pre-login token is tied to anonymous identity and cannot log out a logged-in user.
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/auth/logout", null)).StatusCode);
        await RefreshCsrfAsync(client);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        await RefreshCsrfAsync(client);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/auth/login", new { email, password })).StatusCode);
    }

    [Fact]
    public async Task Viewer_cannot_provision_users_and_admin_can()
    {
        await using var factory = new StockFlowApiFactory(postgres.Database.GetConnectionString());
        foreach (var role in new[] { "Viewer", "Admin" })
        {
            var email = $"{role}-{Guid.NewGuid():N}@example.com";
            var password = $"Valid!{Guid.NewGuid():N}";
            await factory.CreateUserAsync(email, password, role);
            var client = factory.CreateClient();
            await RefreshCsrfAsync(client);
            Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/auth/login", new { email, password })).StatusCode);
            await RefreshCsrfAsync(client);
            var created = await client.PostAsJsonAsync("/api/users", new { email = $"new-{Guid.NewGuid():N}@example.com", password, role = "Warehouse" });
            Assert.Equal(role == "Admin" ? HttpStatusCode.Created : HttpStatusCode.Forbidden, created.StatusCode);
        }
    }

    [Fact]
    public async Task Bootstrap_missing_secret_fails_closed_and_valid_secret_creates_admin_once()
    {
        await using var factory = new StockFlowApiFactory(postgres.Database.GetConnectionString());
        await using var scope = factory.Services.CreateAsyncScope();
        var empty = new ConfigurationBuilder().Build();
        await Assert.ThrowsAsync<InvalidOperationException>(() => DevelopmentSeeder.BootstrapAdminAsync(scope.ServiceProvider, empty));
        var email = $"bootstrap-{Guid.NewGuid():N}@example.com";
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Bootstrap:Email"] = email, ["Bootstrap:Password"] = $"Valid!{Guid.NewGuid():N}" }).Build();
        await DevelopmentSeeder.BootstrapAdminAsync(scope.ServiceProvider, config);
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        Assert.True(await manager.IsInRoleAsync((await manager.FindByEmailAsync(email))!, "Admin"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => DevelopmentSeeder.BootstrapAdminAsync(scope.ServiceProvider, config));
    }

    [Fact]
    public async Task Unknown_api_path_is_404_including_extensionless_routes()
    {
        await using var factory = new StockFlowApiFactory(postgres.Database.GetConnectionString());
        Assert.Equal(HttpStatusCode.NotFound, (await factory.CreateClient().GetAsync("/api/unknown")).StatusCode);
    }
}
