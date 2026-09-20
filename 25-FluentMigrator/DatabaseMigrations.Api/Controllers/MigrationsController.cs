using DatabaseMigrations.Api.Models;
using DatabaseMigrations.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DatabaseMigrations.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MigrationsController : ControllerBase
{
    private readonly IMigrationManager _migrationManager;

    public MigrationsController(IMigrationManager migrationManager)
    {
        _migrationManager = migrationManager;
    }

    /// <summary>
    /// Executes all pending FluentMigrator migrations up to latest version.
    /// </summary>
    [HttpPost("up")]
    [ProducesResponseType(typeof(MigrationResultDto), StatusCodes.Status200OK)]
    public ActionResult<MigrationResultDto> MigrateUp()
    {
        _migrationManager.MigrateUp();
        return Ok(new MigrationResultDto("MigrateUp", null, "All pending migrations applied successfully."));
    }

    /// <summary>
    /// Rolls back migrations down to a specified target version.
    /// </summary>
    [HttpPost("rollback/{version:long}")]
    [ProducesResponseType(typeof(MigrationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<MigrationResultDto> Rollback(long version)
    {
        if (version < 0)
        {
            return BadRequest(new { message = "Target version must be non-negative." });
        }

        _migrationManager.Rollback(version);
        return Ok(new MigrationResultDto("Rollback", version, $"Successfully rolled back database to version {version}."));
    }

    /// <summary>
    /// Gets history of applied migrations from VersionInfo table.
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(IEnumerable<MigrationHistoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MigrationHistoryDto>>> GetHistory()
    {
        var history = await _migrationManager.GetAppliedMigrationsAsync();
        return Ok(history);
    }

    /// <summary>
    /// Inspects SQLite database schema tables and columns to verify migration effects.
    /// </summary>
    [HttpGet("tables")]
    [ProducesResponseType(typeof(IEnumerable<TableInfoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TableInfoDto>>> GetTables()
    {
        var tables = await _migrationManager.GetTablesAsync();
        return Ok(tables);
    }
}
