using CartPromotionEngine.Api.Models;
using CartPromotionEngine.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CartPromotionEngine.Api.Controllers;

[ApiController]
[Route("api/cart")]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ShoppingCart), StatusCodes.Status200OK)]
    public IActionResult GetCart([FromRoute] Guid id)
    {
        var cart = _cartService.GetCart(id);
        return Ok(cart);
    }

    [HttpPost("{id:guid}/items")]
    [ProducesResponseType(typeof(ShoppingCart), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult AddItem([FromRoute] Guid id, [FromBody] AddItemRequest request)
    {
        try
        {
            var cart = _cartService.AddItem(id, request);
            return Ok(cart);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/coupon")]
    [ProducesResponseType(typeof(ShoppingCart), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult ApplyCoupon([FromRoute] Guid id, [FromBody] ApplyCouponRequest request)
    {
        try
        {
            var cart = _cartService.ApplyCoupon(id, request.CouponCode);
            return Ok(cart);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/checkout")]
    [ProducesResponseType(typeof(CheckoutResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Checkout([FromRoute] Guid id, [FromBody] CheckoutRequest request)
    {
        try
        {
            var result = _cartService.Checkout(id, request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
