using Microsoft.AspNetCore.Mvc;
using CachingAdvanced.Api.Services;

namespace CachingAdvanced.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HybridCacheController : ControllerBase
{
    private readonly HybridCacheService _service;

    public HybridCacheController(HybridCacheService service)
    {
        _service = service;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var product = await _service.GetProductAsync(id);
        if (product == null) return NotFound();
        return Ok(product);
    }

    [HttpPost("{id}/invalidate")]
    public async Task<IActionResult> Invalidate(int id)
    {
        await _service.InvalidateAsync(id);
        return Ok();
    }
}
