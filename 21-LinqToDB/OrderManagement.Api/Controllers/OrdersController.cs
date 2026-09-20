using LinqToDB;
using LinqToDB.Data;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Api.Data;
using OrderManagement.Api.Entities;
using OrderManagement.Api.Models;

namespace OrderManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly AppDataConnection _db;

    public OrdersController(AppDataConnection db)
    {
        _db = db;
    }

    /// <summary>
    /// Gets all orders with optional filtering by status and search keyword.
    /// Demonstrates type-safe LINQ query with projection and association counting.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OrderSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<OrderSummaryDto>>> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? search)
    {
        var query = _db.Orders.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(o => o.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(o => o.CustomerName.Contains(search) || o.ShippingAddress.Contains(search));
        }

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new OrderSummaryDto(
                o.Id,
                o.CustomerName,
                o.ShippingAddress,
                o.Status,
                o.TotalAmount,
                o.Items.Count(),
                o.CreatedAt,
                o.UpdatedAt
            ))
            .ToListAsync();

        return Ok(orders);
    }

    /// <summary>
    /// Gets aggregate statistics across all orders directly computed by database.
    /// Demonstrates LinqToDB aggregate functions (Count, Sum, GroupBy) compiled to SQL.
    /// </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(OrderStatisticsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrderStatisticsDto>> GetStatistics()
    {
        var totalOrders = await _db.Orders.CountAsync();
        var totalRevenue = await _db.Orders.SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;
        var totalItemsSold = await _db.OrderItems.SumAsync(i => (int?)i.Quantity) ?? 0;

        var statusGroups = await _db.Orders
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var stats = new OrderStatisticsDto(
            totalOrders,
            totalRevenue,
            totalItemsSold,
            statusGroups.ToDictionary(g => g.Status, g => g.Count)
        );

        return Ok(stats);
    }

    /// <summary>
    /// Gets order details by ID including its items.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(OrderDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDetailDto>> GetById(int id)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id);
        if (order == null)
        {
            return NotFound(new { message = $"Order with ID {id} not found." });
        }

        var items = await _db.OrderItems
            .Where(i => i.OrderId == id)
            .Select(i => new OrderItemDto(
                i.Id,
                i.ProductName,
                i.Quantity,
                i.UnitPrice,
                i.Quantity * i.UnitPrice
            ))
            .ToListAsync();

        var detail = new OrderDetailDto(
            order.Id,
            order.CustomerName,
            order.ShippingAddress,
            order.Status,
            order.TotalAmount,
            order.CreatedAt,
            order.UpdatedAt,
            items
        );

        return Ok(detail);
    }

    /// <summary>
    /// Creates a new order and inserts items in bulk using LinqToDB BulkCopy.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(OrderDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderDetailDto>> Create([FromBody] CreateOrderDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CustomerName))
        {
            return BadRequest(new { message = "Customer name is required." });
        }

        if (string.IsNullOrWhiteSpace(dto.ShippingAddress))
        {
            return BadRequest(new { message = "Shipping address is required." });
        }

        if (dto.Items == null || dto.Items.Count == 0)
        {
            return BadRequest(new { message = "Order must contain at least one item." });
        }

        if (dto.Items.Any(i => i.Quantity <= 0 || i.UnitPrice < 0 || string.IsNullOrWhiteSpace(i.ProductName)))
        {
            return BadRequest(new { message = "Invalid item quantity, price, or product name." });
        }

        var totalAmount = dto.Items.Sum(i => i.Quantity * i.UnitPrice);

        var order = new Order
        {
            CustomerName = dto.CustomerName.Trim(),
            ShippingAddress = dto.ShippingAddress.Trim(),
            Status = "Pending",
            TotalAmount = totalAmount,
            CreatedAt = DateTime.UtcNow
        };

        // LinqToDB InsertWithInt32IdentityAsync
        var orderId = await _db.InsertWithInt32IdentityAsync(order);

        // Prepare items for LinqToDB BulkCopyAsync
        var items = dto.Items.Select(i => new OrderItem
        {
            OrderId = orderId,
            ProductName = i.ProductName.Trim(),
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice
        }).ToList();

        await _db.BulkCopyAsync(items);

        return await GetById(orderId);
    }

    /// <summary>
    /// Updates order status using LinqToDB set-based UPDATE without loading entity into memory.
    /// </summary>
    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(typeof(OrderSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderSummaryDto>> UpdateStatus(int id, [FromBody] UpdateOrderStatusDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Status))
        {
            return BadRequest(new { message = "Status cannot be empty." });
        }

        var now = DateTime.UtcNow;

        // Set-based UPDATE compiled directly to SQL:
        // UPDATE Orders SET Status = @status, UpdatedAt = @now WHERE Id = @id
        var rowsAffected = await _db.Orders
            .Where(o => o.Id == id)
            .Set(o => o.Status, dto.Status.Trim())
            .Set(o => o.UpdatedAt, now)
            .UpdateAsync();

        if (rowsAffected == 0)
        {
            return NotFound(new { message = $"Order with ID {id} not found." });
        }

        var updated = await _db.Orders
            .Where(o => o.Id == id)
            .Select(o => new OrderSummaryDto(
                o.Id,
                o.CustomerName,
                o.ShippingAddress,
                o.Status,
                o.TotalAmount,
                o.Items.Count(),
                o.CreatedAt,
                o.UpdatedAt
            ))
            .FirstAsync();

        return Ok(updated);
    }

    /// <summary>
    /// Deletes order and associated items using LinqToDB set-based DELETE.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        // Delete items first
        await _db.OrderItems
            .Where(i => i.OrderId == id)
            .DeleteAsync();

        var rowsAffected = await _db.Orders
            .Where(o => o.Id == id)
            .DeleteAsync();

        if (rowsAffected == 0)
        {
            return NotFound(new { message = $"Order with ID {id} not found." });
        }

        return NoContent();
    }
}
