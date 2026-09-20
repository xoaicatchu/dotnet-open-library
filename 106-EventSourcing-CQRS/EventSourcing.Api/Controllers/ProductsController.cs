using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EventSourcing.Api.Handlers.Commands;
using EventSourcing.Api.Handlers.Queries;
namespace EventSourcing.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly CreateProductHandler _createHandler;
    private readonly UpdateProductHandler _updateHandler;
    private readonly DeactivateProductHandler _deactivateHandler;
    private readonly GetProductsHandler _getProductsHandler;
    private readonly GetProductByIdHandler _getByIdHandler;

    public ProductsController(
        CreateProductHandler createHandler,
        UpdateProductHandler updateHandler,
        DeactivateProductHandler deactivateHandler,
        GetProductsHandler getProductsHandler,
        GetProductByIdHandler getByIdHandler)
    {
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _deactivateHandler = deactivateHandler;
        _getProductsHandler = getProductsHandler;
        _getByIdHandler = getByIdHandler;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _getProductsHandler.Handle(new GetProductsQuery());
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _getByIdHandler.Handle(new GetProductByIdQuery(id));
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductCommand cmd)
    {
        var id = await _createHandler.Handle(cmd);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductCommand cmd)
    {
        if (id != cmd.Id) return BadRequest();
        try
        {
            await _updateHandler.Handle(cmd);
            return Ok();
        }
        catch (InvalidOperationException)
        {
            var product = await _getByIdHandler.Handle(new GetProductByIdQuery(id));
            if (product == null) return NotFound();
            return BadRequest();
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        try
        {
            await _deactivateHandler.Handle(new DeactivateProductCommand(id));
            return NoContent();
        }
        catch (InvalidOperationException)
        {
            return BadRequest();
        }
    }
}
