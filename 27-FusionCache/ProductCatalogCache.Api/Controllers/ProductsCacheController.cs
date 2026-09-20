using Microsoft.AspNetCore.Mvc;
using ProductCatalogCache.Api.Models;
using ProductCatalogCache.Api.Services;
using ZiggyCreatures.Caching.Fusion;

namespace ProductCatalogCache.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsCacheController : ControllerBase
{
    private readonly IFusionCache _cache;
    private readonly IProductCatalogService _service;

    public ProductsCacheController(IFusionCache cache, IProductCatalogService service)
    {
        _cache = cache;
        _service = service;
    }

    /// <summary>
    /// Gets product by ID with FusionCache: Cache Stampede protection + Fail-Safe.
    /// If multiple concurrent requests arrive, only 1 hits the database.
    /// If the database fails, cached stale data is safely returned (Fail-Safe).
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CachedProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CachedProductResponse>> Get(int id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return BadRequest(new { message = "Product ID must be greater than 0." });
        }

        var product = await _cache.GetOrSetAsync<ProductItem>(
            $"product:{id}",
            async ct => await _service.GetProductFromDatabaseAsync(id, ct),
            options => options
                .SetDuration(TimeSpan.FromMinutes(2))
                .SetFailSafe(true, maxDuration: TimeSpan.FromHours(1), throttleDuration: TimeSpan.FromSeconds(5)),
            cancellationToken
        );

        return Ok(new CachedProductResponse(product, _service.TotalDbQueries));
    }

    /// <summary>
    /// Evicts the product from cache to force subsequent request to hit the database.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Evict(int id)
    {
        await _cache.RemoveAsync($"product:{id}");
        return NoContent();
    }

    /// <summary>
    /// Simulates database outage / network failure to demonstrate FusionCache Fail-Safe behavior.
    /// </summary>
    [HttpPost("simulate-failure")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult SimulateFailure([FromQuery] bool enabled = true)
    {
        _service.SetFailureSimulation(enabled);
        return Ok(new { message = $"Database failure simulation set to {enabled}." });
    }

    /// <summary>
    /// Gets current cache & database query metrics.
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(CacheStatsDto), StatusCodes.Status200OK)]
    public ActionResult<CacheStatsDto> GetStats()
    {
        return Ok(new CacheStatsDto(_service.TotalDbQueries, _service.IsFailureSimulated));
    }

    /// <summary>
    /// Resets metrics and flags for testing.
    /// </summary>
    [HttpPost("reset")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Reset()
    {
        _service.ResetStats();
        return Ok(new { message = "Stats reset." });
    }
}
