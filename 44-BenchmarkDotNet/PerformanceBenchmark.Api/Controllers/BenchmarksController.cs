using Microsoft.AspNetCore.Mvc;
using PerformanceBenchmark.Api.Models;
using PerformanceBenchmark.Api.Services;

namespace PerformanceBenchmark.Api.Controllers;

[ApiController]
[Route("api/benchmarks")]
public class BenchmarksController : ControllerBase
{
    private readonly IBenchmarkRunnerService _runnerService;

    public BenchmarksController(IBenchmarkRunnerService runnerService)
    {
        _runnerService = runnerService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<BenchmarkMetadata>), StatusCodes.Status200OK)]
    public IActionResult GetAvailableBenchmarks()
    {
        var benchmarks = _runnerService.GetAvailableBenchmarks();
        return Ok(benchmarks);
    }

    [HttpPost("run")]
    [ProducesResponseType(typeof(BenchmarkComparisonResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult RunComparison([FromBody] RunBenchmarkRequest request)
    {
        try
        {
            var result = _runnerService.RunComparison(request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
