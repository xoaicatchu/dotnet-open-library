using DatabaseMigrations.Api.Models;

namespace DatabaseMigrations.Api.Services;

public interface IMigrationManager
{
    void MigrateUp();
    void Rollback(long targetVersion);
    Task<IReadOnlyList<MigrationHistoryDto>> GetAppliedMigrationsAsync();
    Task<IReadOnlyList<TableInfoDto>> GetTablesAsync();
}
