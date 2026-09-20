using Microsoft.AspNetCore.Mvc;
using CachingAdvanced.Api.Services;

namespace CachingAdvanced.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MemoryCacheController : ControllerBase
{
    private readonly MemoryCacheService _service;

    public MemoryCacheController(MemoryCacheService service)
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
    public IActionResult Invalidate(int id)
    {
        _service.Invalidate(id);
        return Ok();
    }
}
