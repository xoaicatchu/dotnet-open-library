using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using SwaggerDocumentation.Api.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace SwaggerDocumentation.Api.Controllers.V1;

/// <summary>
/// Quản lý danh mục sản phẩm (Phiên bản V1).
/// </summary>
[ApiController]
[ApiExplorerSettings(GroupName = "v1")]
[Route("api/v1/products")]
[Produces("application/json")]
public class ProductsV1Controller : ControllerBase
{
    private static readonly ConcurrentDictionary<int, ProductV1> Products = new()
    {
        [1] = new(1, "Laptop Dell XPS 15", 1500.00m, "Computers"),
        [2] = new(2, "Apple iPad Pro", 999.00m, "Tablets")
    };

    /// <summary>
    /// Lấy toàn bộ danh sách sản phẩm phiên bản V1.
    /// </summary>
    /// <returns>Danh sách sản phẩm V1</returns>
    /// <response code="200">Trả về danh sách sản phẩm thành công</response>
    [HttpGet]
    [SwaggerOperation(
        Summary = "Lấy danh sách sản phẩm V1",
        Description = "Trả về toàn bộ các sản phẩm hiện có trong hệ thống theo định dạng phiên bản 1.",
        OperationId = "GetProductsV1"
    )]
    [ProducesResponseType(typeof(List<ProductV1>), StatusCodes.Status200OK)]
    public IActionResult GetAll()
    {
        return Ok(Products.Values.ToList());
    }

    /// <summary>
    /// Lấy chi tiết sản phẩm theo mã định danh (Id).
    /// </summary>
    /// <param name="id">Mã số nguyên của sản phẩm</param>
    /// <returns>Thông tin chi tiết sản phẩm</returns>
    /// <response code="200">Tìm thấy sản phẩm</response>
    /// <response code="404">Không tìm thấy sản phẩm với Id cung cấp</response>
    [HttpGet("{id:int}")]
    [SwaggerOperation(Summary = "Lấy chi tiết sản phẩm theo Id", OperationId = "GetProductV1ById")]
    [ProducesResponseType(typeof(ProductV1), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetById([FromRoute] int id)
    {
        if (Products.TryGetValue(id, out var product))
        {
            return Ok(product);
        }

        return NotFound(new { error = $"Không tìm thấy sản phẩm với Id: {id}" });
    }

    /// <summary>
    /// Tạo mới một sản phẩm phiên bản V1.
    /// </summary>
    /// <param name="request">Thông tin sản phẩm cần tạo</param>
    /// <returns>Sản phẩm vừa được tạo</returns>
    /// <response code="201">Tạo sản phẩm thành công</response>
    /// <response code="400">Dữ liệu yêu cầu không hợp lệ</response>
    [HttpPost]
    [SwaggerOperation(Summary = "Tạo mới sản phẩm V1", OperationId = "CreateProductV1")]
    [ProducesResponseType(typeof(ProductV1), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Create([FromBody] CreateProductV1Request request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { error = "Tên sản phẩm không được để trống." });
        }

        if (request.Price < 0)
        {
            return BadRequest(new { error = "Giá sản phẩm không được âm." });
        }

        int newId = Products.Keys.DefaultIfEmpty(0).Max() + 1;
        var product = new ProductV1(newId, request.Name, request.Price, request.Category);
        Products[newId] = product;

        return CreatedAtAction(nameof(GetById), new { id = newId }, product);
    }
}
