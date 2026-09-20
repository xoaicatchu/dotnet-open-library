using Duende.IdentityServer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InventoryIdServer.Api.Models;

namespace InventoryIdServer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(IdentityServerConstants.LocalApi.PolicyName)]
public class InventoryController : ControllerBase
{
    private static readonly List<InventoryItemDto> Items =
    [
        new("SKU-LAPTOP-01", "Enterprise Laptop 16-inch", 25, 1499.99m),
        new("SKU-MONITOR-02", "4K UltraWide Monitor", 40, 699.50m),
        new("SKU-KEYBOARD-03", "Mechanical Wireless Keyboard", 100, 119.00m)
    ];

    /// <summary>
    /// Retrieves the current inventory items. Protected by Duende IdentityServer Local API policy.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<InventoryItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetAll()
    {
        return Ok(Items);
    }

    /// <summary>
    /// Retrieves a specific inventory item by SKU.
    /// </summary>
    [HttpGet("{sku}")]
    [ProducesResponseType(typeof(InventoryItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetBySku(string sku)
    {
        var item = Items.FirstOrDefault(i => i.Sku.Equals(sku, StringComparison.OrdinalIgnoreCase));
        if (item == null)
        {
            return NotFound(new { message = $"Item with SKU '{sku}' was not found in inventory." });
        }
        return Ok(item);
    }

    /// <summary>
    /// Adds a new inventory item.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(InventoryItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Create([FromBody] InventoryItemDto newItem)
    {
        if (string.IsNullOrWhiteSpace(newItem.Sku) || string.IsNullOrWhiteSpace(newItem.Name))
        {
            return BadRequest(new { message = "Sku and Name are required." });
        }

        if (Items.Any(i => i.Sku.Equals(newItem.Sku, StringComparison.OrdinalIgnoreCase)))
        {
            return BadRequest(new { message = $"Item with SKU '{newItem.Sku}' already exists." });
        }

        Items.Add(newItem);
        return CreatedAtAction(nameof(GetBySku), new { sku = newItem.Sku }, newItem);
    }
}
