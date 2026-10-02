using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StockFlow.Api.Data;

namespace StockFlow.Tests.Infrastructure;

public sealed class PostgresFixture : IAsyncLifetime
{
    public PostgreSqlContainer Database { get; } = new PostgreSqlBuilder("postgres:17-alpine").Build();
    public async Task InitializeAsync()
    {
        await Database.StartAsync();
        await using var db = new StockFlowDbContext(new DbContextOptionsBuilder<StockFlowDbContext>().UseNpgsql(Database.GetConnectionString()).Options);
        await db.Database.MigrateAsync();
    }
    public Task DisposeAsync() => Database.DisposeAsync().AsTask();
}

[CollectionDefinition("Postgres")]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>;

public sealed class StockFlowApiFactory(string connectionString, string environment = "Testing", Dictionary<string, string?>? settings = null, string? webRoot = null) : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Minimal-host startup reads certificate configuration before deferred web callbacks execute.
        builder.ConfigureHostConfiguration(config => config.AddInMemoryCollection(settings ?? []));
        return base.CreateHost(builder);
    }
    public async Task<string> CreateUserAsync(string email, string password, string role)
    {
        await using var scope = Services.CreateAsyncScope();
        await DevelopmentSeeder.EnsureRolesAsync(scope.ServiceProvider);
        var manager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Microsoft.AspNetCore.Identity.IdentityUser>>();
        var user = new Microsoft.AspNetCore.Identity.IdentityUser { UserName = email, Email = email };
        var result = await manager.CreateAsync(user, password);
        Assert.True(result.Succeeded, string.Join(";", result.Errors.Select(x => x.Description)));
        Assert.True((await manager.AddToRoleAsync(user, role)).Succeeded);
        return user.Id;
    }
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        if (webRoot is not null) builder.UseWebRoot(webRoot);
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:StockFlow"] = connectionString
        }).AddInMemoryCollection(settings ?? []));
    }
}
