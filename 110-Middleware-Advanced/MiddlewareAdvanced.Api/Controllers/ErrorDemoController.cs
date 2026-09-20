using Microsoft.AspNetCore.Mvc;
using MiddlewareAdvanced.Api.ExceptionHandling;

namespace MiddlewareAdvanced.Api.Controllers;

[ApiController]
[Route("api/demo")]
public class ErrorDemoController : ControllerBase
{
    [HttpGet("not-found")]
    public IActionResult ThrowNotFound() 
        => throw new KeyNotFoundException("Product not found");
    
    [HttpGet("business-error")]
    public IActionResult ThrowBusiness() 
        => throw new BusinessException("Insufficient inventory");
    
    [HttpGet("server-error")]
    public IActionResult ThrowServer() 
        => throw new InvalidOperationException("Unexpected server failure");
    
    [HttpGet("correlation-id")]
    public IActionResult GetCorrelationId()
    {
        var correlationId = HttpContext.Items["CorrelationId"] as string;
        return Ok(new { CorrelationId = correlationId });
    }
    
    [HttpGet("slow-endpoint")]
    public async Task<IActionResult> SlowEndpoint()
    {
        await Task.Delay(200);
        return Ok(new { Message = "Slow response", TimeMs = 200 });
    }
}
