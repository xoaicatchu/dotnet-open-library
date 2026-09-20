using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using SqlMigrationsDbUp.Api.Models;

namespace SqlMigrationsDbUp.Tests;

public class UpgradeTestsFixture : WebApplicationFactory<Program>
{
    private readonly string _dbPath;

    public UpgradeTestsFixture()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"dbup_test_{Guid.NewGuid():N}.db");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={_dbPath}");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (File.Exists(_dbPath))
        {
            try { File.Delete(_dbPath); } catch { }
        }
    }
}

public class DatabaseUpgradeTests : IClassFixture<UpgradeTestsFixture>
{
    private readonly HttpClient _client;

    public DatabaseUpgradeTests(UpgradeTestsFixture fixture)
    {
        _client = fixture.CreateClient();
    }

    [Fact]
    public async Task CompleteUpgradeLifecycle_Status_Upgrade_VerifyTables()
    {
        // 1. Initial status: scripts should be pending
        var initStatusRes = await _client.GetAsync("/api/databaseupgrades/status");
        Assert.Equal(HttpStatusCode.OK, initStatusRes.StatusCode);
        var initStatus = await initStatusRes.Content.ReadFromJsonAsync<UpgradeStatusDto>();
        Assert.NotNull(initStatus);
        Assert.True(initStatus.IsUpgradeRequired);
        Assert.NotEmpty(initStatus.PendingScripts);
        Assert.Empty(initStatus.ExecutedScripts);

        // 2. Perform Upgrade
        var upgradeRes = await _client.PostAsync("/api/databaseupgrades/upgrade", null);
        Assert.Equal(HttpStatusCode.OK, upgradeRes.StatusCode);
        var upgradeResult = await upgradeRes.Content.ReadFromJsonAsync<UpgradeResultDto>();
        Assert.NotNull(upgradeResult);
        Assert.True(upgradeResult.Successful);
        Assert.Equal(3, upgradeResult.ScriptsExecutedCount);

        // 3. Status after upgrade: no pending scripts
        var postStatusRes = await _client.GetAsync("/api/databaseupgrades/status");
        Assert.Equal(HttpStatusCode.OK, postStatusRes.StatusCode);
        var postStatus = await postStatusRes.Content.ReadFromJsonAsync<UpgradeStatusDto>();
        Assert.NotNull(postStatus);
        Assert.False(postStatus.IsUpgradeRequired);
        Assert.Empty(postStatus.PendingScripts);
        Assert.Equal(3, postStatus.ExecutedScripts.Count);

        // 4. Verify Tables and seeded data
        var tablesRes = await _client.GetAsync("/api/databaseupgrades/tables");
        Assert.Equal(HttpStatusCode.OK, tablesRes.StatusCode);
        var tables = await tablesRes.Content.ReadFromJsonAsync<List<TableInfoDto>>();
        Assert.NotNull(tables);
        Assert.Contains(tables, t => t.TableName == "Invoices");
        Assert.Contains(tables, t => t.TableName == "InvoiceItems");
        Assert.Contains(tables, t => t.TableName == "SchemaVersions");

        var invoicesTable = tables.First(t => t.TableName == "Invoices");
        Assert.Equal(1, invoicesTable.RowCount); // Seeded 1 invoice
    }
}
