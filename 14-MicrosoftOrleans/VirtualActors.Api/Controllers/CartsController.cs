using Microsoft.AspNetCore.Mvc;
using VirtualActors.Api.Grains;
using VirtualActors.Api.Models;

namespace VirtualActors.Api.Controllers;

[ApiController]
[Route("api/carts")]
public class CartsController : ControllerBase
{
    private readonly IGrainFactory _grainFactory;

    public CartsController(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
    }

    [HttpPost("{cartId}/items")]
    public async Task<IActionResult> AddItem(string cartId, [FromBody] CartItem item)
    {
        var cart = _grainFactory.GetGrain<ICartGrain>(cartId);
        await cart.AddItem(item.Sku, item.Name, item.Quantity, item.UnitPrice);
        var summary = await cart.GetCart();
        return Ok(summary);
    }

    [HttpGet("{cartId}")]
    public async Task<IActionResult> GetCart(string cartId)
    {
        var cart = _grainFactory.GetGrain<ICartGrain>(cartId);
        var summary = await cart.GetCart();
        return Ok(summary);
    }

    [HttpDelete("{cartId}/items/{sku}")]
    public async Task<IActionResult> RemoveItem(string cartId, string sku)
    {
        var cart = _grainFactory.GetGrain<ICartGrain>(cartId);
        await cart.RemoveItem(sku);
        return Ok();
    }

    [HttpDelete("{cartId}")]
    public async Task<IActionResult> ClearCart(string cartId)
    {
        var cart = _grainFactory.GetGrain<ICartGrain>(cartId);
        await cart.ClearCart();
        return NoContent();
    }
}
