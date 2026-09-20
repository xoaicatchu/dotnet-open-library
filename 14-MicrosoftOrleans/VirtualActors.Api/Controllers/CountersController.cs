using Microsoft.AspNetCore.Mvc;
using VirtualActors.Api.Grains;

namespace VirtualActors.Api.Controllers;

[ApiController]
[Route("api/counters")]
public class CountersController : ControllerBase
{
    private readonly IGrainFactory _grainFactory;

    public CountersController(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
    }

    [HttpPost("{counterId}/increment")]
    public async Task<IActionResult> Increment(string counterId, [FromQuery] int? val)
    {
        int incrementValue = val ?? 1;
        var counter = _grainFactory.GetGrain<ICounterGrain>(counterId);
        var newCount = await counter.Increment(incrementValue);
        return Ok(new { counterId = counterId, count = newCount });
    }

    [HttpGet("{counterId}")]
    public async Task<IActionResult> GetCount(string counterId)
    {
        var counter = _grainFactory.GetGrain<ICounterGrain>(counterId);
        var count = await counter.GetCount();
        return Ok(count);
    }
}
