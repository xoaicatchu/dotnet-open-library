using System.Net;
using System.Net.Http.Json;
using DatabaseMigrations.Api.Models;
using DatabaseMigrations.Api.Services;
using FluentMigrator.Runner;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace DatabaseMigrations.Tests;

public class MigrationTestsFixture : WebApplicationFactory<Program>
{
    private readonly string _dbPath;

    public MigrationTestsFixture()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"migrations_test_{Guid.NewGuid():N}.db");
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

public class MigrationTests : IClassFixture<MigrationTestsFixture>
{
    private readonly HttpClient _client;

    public MigrationTests(MigrationTestsFixture fixture)
    {
        _client = fixture.CreateClient();
    }

    [Fact]
    public async Task CompleteMigrationLifecycle_Up_History_Tables_Rollback()
    {
        // 1. Initial state: tables should not have Users or Profiles yet
        var initTablesRes = await _client.GetAsync("/api/migrations/tables");
        Assert.Equal(HttpStatusCode.OK, initTablesRes.StatusCode);
        var initTables = await initTablesRes.Content.ReadFromJsonAsync<List<TableInfoDto>>();
        Assert.NotNull(initTables);
        Assert.DoesNotContain(initTables, t => t.TableName == "Users");

        // 2. Migrate Up
        var upRes = await _client.PostAsync("/api/migrations/up", null);
        Assert.Equal(HttpStatusCode.OK, upRes.StatusCode);
        var upResult = await upRes.Content.ReadFromJsonAsync<MigrationResultDto>();
        Assert.NotNull(upResult);
        Assert.Equal("MigrateUp", upResult.Action);

        // 3. Check History
        var historyRes = await _client.GetAsync("/api/migrations/history");
        Assert.Equal(HttpStatusCode.OK, historyRes.StatusCode);
        var history = await historyRes.Content.ReadFromJsonAsync<List<MigrationHistoryDto>>();
        Assert.NotNull(history);
        Assert.Equal(3, history.Count);
        Assert.Contains(history, h => h.Version == 202601010001);
        Assert.Contains(history, h => h.Version == 202601010002);
        Assert.Contains(history, h => h.Version == 202601010003);

        // 4. Check Tables after Migrate Up
        var tablesRes = await _client.GetAsync("/api/migrations/tables");
        Assert.Equal(HttpStatusCode.OK, tablesRes.StatusCode);
        var tables = await tablesRes.Content.ReadFromJsonAsync<List<TableInfoDto>>();
        Assert.NotNull(tables);
        Assert.Contains(tables, t => t.TableName == "Users");
        Assert.Contains(tables, t => t.TableName == "Profiles");

        var usersTable = tables.First(t => t.TableName == "Users");
        Assert.Contains(usersTable.Columns, c => c.StartsWith("Status"));

        // 5. Rollback to version 202601010001 (should remove Profiles table and Status column)
        var rollbackRes = await _client.PostAsync("/api/migrations/rollback/202601010001", null);
        Assert.Equal(HttpStatusCode.OK, rollbackRes.StatusCode);

        // 6. Check Tables after Rollback
        var postRollbackTablesRes = await _client.GetAsync("/api/migrations/tables");
        var postRollbackTables = await postRollbackTablesRes.Content.ReadFromJsonAsync<List<TableInfoDto>>();
        Assert.NotNull(postRollbackTables);
        Assert.Contains(postRollbackTables, t => t.TableName == "Users");
        Assert.DoesNotContain(postRollbackTables, t => t.TableName == "Profiles");

        var rolledBackUsersTable = postRollbackTables.First(t => t.TableName == "Users");
        Assert.DoesNotContain(rolledBackUsersTable.Columns, c => c.StartsWith("Status"));
    }

    [Fact]
    public async Task Rollback_WithNegativeVersion_ReturnsBadRequest()
    {
        var response = await _client.PostAsync("/api/migrations/rollback/-5", null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
