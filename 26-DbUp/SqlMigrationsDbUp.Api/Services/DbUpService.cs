using System.Reflection;
using DbUp;
using DbUp.Engine;
using Microsoft.Data.Sqlite;
using SqlMigrationsDbUp.Api.Models;

namespace SqlMigrationsDbUp.Api.Services;

public interface IDbUpService
{
    UpgradeResultDto PerformUpgrade();
    UpgradeStatusDto GetStatus();
    Task<List<TableInfoDto>> GetTablesAsync();
}

public class DbUpService : IDbUpService
{
    private readonly string _connectionString;

    public DbUpService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
                           ?? "Data Source=invoices.db";
    }

    private UpgradeEngine CreateUpgradeEngine()
    {
        return DeployChanges.To
            .SqliteDatabase(_connectionString)
            .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
            .LogToConsole()
            .Build();
    }

    public UpgradeResultDto PerformUpgrade()
    {
        var engine = CreateUpgradeEngine();
        var scriptsToExecute = engine.GetScriptsToExecute().Select(s => s.Name).ToList();
        var result = engine.PerformUpgrade();

        return new UpgradeResultDto(
            result.Successful,
            scriptsToExecute.Count,
            scriptsToExecute,
            result.Error?.Message
        );
    }

    public UpgradeStatusDto GetStatus()
    {
        var engine = CreateUpgradeEngine();
        var isRequired = engine.IsUpgradeRequired();
        var executed = engine.GetExecutedScripts().ToList();
        var pending = engine.GetScriptsToExecute().Select(s => s.Name).ToList();

        return new UpgradeStatusDto(isRequired, executed, pending);
    }

    public async Task<List<TableInfoDto>> GetTablesAsync()
    {
        var tables = new List<TableInfoDto>();
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name ASC;";
        var tableNames = new List<string>();
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                tableNames.Add(reader.GetString(0));
            }
        }

        foreach (var tableName in tableNames)
        {
            using var pragmaCmd = connection.CreateCommand();
            pragmaCmd.CommandText = $"PRAGMA table_info('{tableName}');";
            var columns = new List<string>();
            using (var pragmaReader = await pragmaCmd.ExecuteReaderAsync())
            {
                while (await pragmaReader.ReadAsync())
                {
                    columns.Add($"{pragmaReader.GetString(1)} ({pragmaReader.GetString(2)})");
                }
            }

            using var countCmd = connection.CreateCommand();
            countCmd.CommandText = $"SELECT COUNT(*) FROM '{tableName}';";
            var rowCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

            tables.Add(new TableInfoDto(tableName, rowCount, columns));
        }

        return tables;
    }
}
