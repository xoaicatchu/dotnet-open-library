using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Entities;
using OrderService.Infrastructure;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrderDbContext _db;
    private readonly IProductServiceClient _productClient;

    public OrdersController(OrderDbContext db, IProductServiceClient productClient)
    {
        _db = db;
        _productClient = productClient;
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        return Ok(await _db.Orders.Include(o => o.Items).ToListAsync());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(int id)
    {
        var o = await _db.Orders.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id);
        if (o == null) return NotFound();
        return Ok(o);
    }

    public class CreateOrderDto { public int ProductId { get; set; } public int Quantity { get; set; } }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
    {
        var (available, msg) = await _productClient.CheckStockAsync(dto.ProductId, dto.Quantity);
        if (!available) return BadRequest(msg);
        
        var order = new OrderEntity
        {
            CreatedAt = DateTime.UtcNow,
            Items = new List<OrderItemEntity> { new OrderItemEntity { ProductId = dto.ProductId, Quantity = dto.Quantity } }
        };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        
        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
    }
}
