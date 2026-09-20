using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using SwaggerDocumentation.Api.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace SwaggerDocumentation.Api.Controllers.V2;

/// <summary>
/// Quản lý danh mục sản phẩm nâng cao (Phiên bản V2).
/// </summary>
[ApiController]
[ApiExplorerSettings(GroupName = "v2")]
[Route("api/v2/products")]
[Produces("application/json")]
public class ProductsV2Controller : ControllerBase
{
    private static readonly ConcurrentDictionary<int, ProductV2> Products = new()
    {
        [1] = new(1, "DELL-XPS-15", "Laptop Dell XPS 15 OLED", 1899.00m, "Computers", 4.8, true, ["laptop", "oled", "intel"]),
        [2] = new(2, "APPL-IPAD-PRO", "Apple iPad Pro M4", 1199.00m, "Tablets", 4.9, true, ["apple", "tablet", "m4"])
    };

    /// <summary>
    /// Lấy toàn bộ danh sách sản phẩm phiên bản V2 với thuộc tính nâng cao.
    /// </summary>
    /// <returns>Danh sách sản phẩm V2</returns>
    /// <response code="200">Trả về danh sách sản phẩm thành công</response>
    [HttpGet]
    [SwaggerOperation(
        Summary = "Lấy danh sách sản phẩm V2",
        Description = "Trả về toàn bộ các sản phẩm bao gồm SKU, đánh giá sao (Rating), trạng thái tồn kho và tags.",
        OperationId = "GetProductsV2"
    )]
    [ProducesResponseType(typeof(List<ProductV2>), StatusCodes.Status200OK)]
    public IActionResult GetAll()
    {
        return Ok(Products.Values.ToList());
    }

    /// <summary>
    /// Lấy chi tiết sản phẩm V2 theo Id.
    /// </summary>
    /// <param name="id">Mã số nguyên của sản phẩm</param>
    /// <returns>Thông tin chi tiết sản phẩm V2</returns>
    [HttpGet("{id:int}")]
    [SwaggerOperation(Summary = "Lấy chi tiết sản phẩm V2 theo Id", OperationId = "GetProductV2ById")]
    [ProducesResponseType(typeof(ProductV2), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetById([FromRoute] int id)
    {
        if (Products.TryGetValue(id, out var product))
        {
            return Ok(product);
        }

        return NotFound(new { error = $"Không tìm thấy sản phẩm V2 với Id: {id}" });
    }

    /// <summary>
    /// Tạo mới một sản phẩm phiên bản V2 với đầy đủ metadata.
    /// </summary>
    /// <param name="request">Dữ liệu sản phẩm V2</param>
    /// <returns>Sản phẩm V2 vừa tạo</returns>
    [HttpPost]
    [SwaggerOperation(Summary = "Tạo mới sản phẩm V2", OperationId = "CreateProductV2")]
    [ProducesResponseType(typeof(ProductV2), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Create([FromBody] CreateProductV2Request request)
    {
        if (string.IsNullOrWhiteSpace(request.Sku))
        {
            return BadRequest(new { error = "Mã SKU không được để trống." });
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { error = "Tên sản phẩm không được để trống." });
        }

        int newId = Products.Keys.DefaultIfEmpty(0).Max() + 1;
        var product = new ProductV2(
            newId,
            request.Sku,
            request.Name,
            request.Price,
            request.Category,
            5.0,
            request.InStock,
            request.Tags ?? []
        );

        Products[newId] = product;
        return CreatedAtAction(nameof(GetById), new { id = newId }, product);
    }
}
