using Microsoft.AspNetCore.Mvc;
using MiddlewareAdvanced.Api.ExceptionHandling;

namespace MiddlewareAdvanced.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    [HttpGet] 
    public IActionResult GetAll() => Ok(new[] { new { Id = 1, Name = "Product A" } });
    
    [HttpPost] 
    public IActionResult Create([FromBody] CreateRequest req)
    {
        if (string.IsNullOrEmpty(req.Name)) 
            throw new BusinessException("Product name is required");
        return CreatedAtAction(nameof(GetAll), new { Id = 1, req.Name });
    }
}

public record CreateRequest(string Name);
