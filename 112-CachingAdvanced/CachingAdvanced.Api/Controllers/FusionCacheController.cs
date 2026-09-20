using Microsoft.AspNetCore.Mvc;
using CachingAdvanced.Api.Services;

namespace CachingAdvanced.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FusionCacheController : ControllerBase
{
    private readonly FusionCacheService _service;

    public FusionCacheController(FusionCacheService service)
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
}
