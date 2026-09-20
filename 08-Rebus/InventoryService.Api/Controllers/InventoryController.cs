using InventoryService.Api.Data;
using InventoryService.Api.Messages;
using InventoryService.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Rebus.Bus;

namespace InventoryService.Api.Controllers;

public record CreateInventoryRequest(string Sku, string Name, int Quantity);
public record StockRequest(int Amount);

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly InventoryStore _store;
    private readonly IBus _bus;

    public InventoryController(InventoryStore store, IBus bus)
    {
        _store = store;
        _bus = bus;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(_store.GetAll());
    }

    [HttpGet("{id:int}")]
    public IActionResult GetById(int id)
    {
        var item = _store.GetById(id);
        return item is not null ? Ok(item) : NotFound();
    }

    [HttpPost]
    public IActionResult Create([FromBody] CreateInventoryRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Sku) || req.Sku.Length > 50) return BadRequest(new { Message = "Invalid SKU" });
        if (string.IsNullOrWhiteSpace(req.Name) || req.Name.Length > 200) return BadRequest(new { Message = "Invalid Name" });
        if (req.Quantity < 0) return BadRequest(new { Message = "Invalid Quantity" });

        var item = new InventoryItem
        {
            Sku = req.Sku,
            Name = req.Name,
            Quantity = req.Quantity
        };

        var created = _store.Add(item);
        return Created($"/api/inventory/{created.Id}", created);
    }

    [HttpPost("{id:int}/add-stock")]
    public async Task<IActionResult> AddStock(int id, [FromBody] StockRequest req)
    {
        if (req.Amount <= 0) return BadRequest(new { Message = "Amount must be greater than 0" });
        if (_store.GetById(id) is null) return NotFound();
        
        await _bus.Send(new AddStockCommand(id, req.Amount));
        return Accepted();
    }

    [HttpPost("{id:int}/remove-stock")]
    public async Task<IActionResult> RemoveStock(int id, [FromBody] StockRequest req)
    {
        if (req.Amount <= 0) return BadRequest(new { Message = "Amount must be greater than 0" });
        if (_store.GetById(id) is null) return NotFound();
        
        await _bus.Send(new RemoveStockCommand(id, req.Amount));
        return Accepted();
    }
}
