using Microsoft.AspNetCore.Mvc;
using SqlMigrationsDbUp.Api.Models;
using SqlMigrationsDbUp.Api.Services;

namespace SqlMigrationsDbUp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DatabaseUpgradesController : ControllerBase
{
    private readonly IDbUpService _dbUpService;

    public DatabaseUpgradesController(IDbUpService dbUpService)
    {
        _dbUpService = dbUpService;
    }

    /// <summary>
    /// Executes all pending embedded SQL migration scripts in order.
    /// </summary>
    [HttpPost("upgrade")]
    [ProducesResponseType(typeof(UpgradeResultDto), StatusCodes.Status200OK)]
    public ActionResult<UpgradeResultDto> Upgrade()
    {
        var result = _dbUpService.PerformUpgrade();
        return Ok(result);
    }

    /// <summary>
    /// Checks upgrade status: executed scripts vs pending scripts.
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(UpgradeStatusDto), StatusCodes.Status200OK)]
    public ActionResult<UpgradeStatusDto> GetStatus()
    {
        var status = _dbUpService.GetStatus();
        return Ok(status);
    }

    /// <summary>
    /// Inspects SQLite database tables, column definitions, and row counts.
    /// </summary>
    [HttpGet("tables")]
    [ProducesResponseType(typeof(IEnumerable<TableInfoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TableInfoDto>>> GetTables()
    {
        var tables = await _dbUpService.GetTablesAsync();
        return Ok(tables);
    }
}
