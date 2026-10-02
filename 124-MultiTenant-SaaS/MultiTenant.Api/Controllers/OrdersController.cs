using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiTenant.Api.Core.Entities;
using MultiTenant.Api.Core.Interfaces;
using MultiTenant.Api.Infrastructure.Context;

namespace MultiTenant.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;

    public OrdersController(ApplicationDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Lấy danh sách đơn hàng của Tenant hiện tại.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Order>>> GetAll()
    {
        var orders = await _db.Orders
            .OrderByDescending(o => o.CreatedAtUtc)
            .ToListAsync();

        return Ok(new
        {
            tenantId = _tenantContext.TenantId,
            count = orders.Count,
            data = orders
        });
    }

    /// <summary>
    /// Tạo đơn hàng mới cho Tenant hiện tại.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Order>> Create([FromBody] CreateOrderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerName) || request.TotalAmount <= 0)
        {
            return BadRequest(new { error = "Tên khách hàng và Tổng tiền hợp lệ là bắt buộc." });
        }

        var order = new Order
        {
            CustomerName = request.CustomerName.Trim(),
            TotalAmount = request.TotalAmount,
            Status = "Processing"
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { id = order.Id }, order);
    }
}

public record CreateOrderRequest(
    string CustomerName,
    decimal TotalAmount
);
