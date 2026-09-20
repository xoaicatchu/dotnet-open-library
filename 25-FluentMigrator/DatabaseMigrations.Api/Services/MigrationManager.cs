using DatabaseMigrations.Api.Models;
using FluentMigrator.Runner;
using Microsoft.Data.Sqlite;

namespace DatabaseMigrations.Api.Services;

public class MigrationManager : IMigrationManager
{
    private readonly IMigrationRunner _runner;
    private readonly string _connectionString;

    public MigrationManager(IMigrationRunner runner, IConfiguration configuration)
    {
        _runner = runner;
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=migrations.db";
    }

    public void MigrateUp()
    {
        _runner.MigrateUp();
    }

    public void Rollback(long targetVersion)
    {
        _runner.MigrateDown(targetVersion);
    }

    public async Task<IReadOnlyList<MigrationHistoryDto>> GetAppliedMigrationsAsync()
    {
        var list = new List<MigrationHistoryDto>();
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        // Check if VersionInfo table exists
        using var checkCmd = connection.CreateCommand();
        checkCmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='VersionInfo';";
        var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;
        if (!exists) return list;

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Version, AppliedOn, Description FROM VersionInfo ORDER BY Version ASC;";
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var version = reader.GetInt64(0);
            var appliedOn = reader.IsDBNull(1) ? DateTime.MinValue : reader.GetDateTime(1);
            var description = reader.IsDBNull(2) ? null : reader.GetString(2);
            list.Add(new MigrationHistoryDto(version, appliedOn, description));
        }

        return list;
    }

    public async Task<IReadOnlyList<TableInfoDto>> GetTablesAsync()
    {
        var tables = new List<TableInfoDto>();
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        // Get user tables (excluding sqlite internal tables)
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
                    var colName = pragmaReader.GetString(1);
                    var colType = pragmaReader.GetString(2);
                    columns.Add($"{colName} ({colType})");
                }
            }
            tables.Add(new TableInfoDto(tableName, columns.Count, columns));
        }

        return tables;
    }
}
