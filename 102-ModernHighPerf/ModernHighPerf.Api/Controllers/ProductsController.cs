using Microsoft.AspNetCore.Mvc;
using Wolverine;
using ModernHighPerf.Api.Features.Products;

namespace ModernHighPerf.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IMessageBus _bus;
    public ProductsController(IMessageBus bus) => _bus = bus;
    
    [HttpGet]
    public async Task<ActionResult<List<ProductSummaryDto>>> GetAll()
        => Ok(await _bus.InvokeAsync<List<ProductSummaryDto>>(new GetProductsQuery()));

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDetailDto>> GetById(int id)
    {
        var result = await _bus.InvokeAsync<ProductDetailDto?>(new GetProductByIdQuery(id));
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ProductDetailDto>> Create([FromBody] CreateProductRequest request)
    {
        var result = await _bus.InvokeAsync<ProductDetailDto>(new CreateProductCommand(request));
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ProductDetailDto>> Update(int id, [FromBody] UpdateProductRequest request)
    {
        var result = await _bus.InvokeAsync<ProductDetailDto?>(new UpdateProductCommand(id, request));
        if (result == null) return NotFound();
        return Ok(result);
    }
}
