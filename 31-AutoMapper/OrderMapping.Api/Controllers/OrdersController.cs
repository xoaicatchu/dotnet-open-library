using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using OrderMapping.Api.Data;
using OrderMapping.Api.Entities;
using OrderMapping.Api.Models;

namespace OrderMapping.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IMapper _mapper;
    private readonly OrderStore _store;

    public OrdersController(IMapper mapper, OrderStore store)
    {
        _mapper = mapper;
        _store = store;
    }

    /// <summary>
    /// Gets all orders projected to OrderSummaryDto (flattened customer name & calculated totals).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OrderSummaryDto>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<OrderSummaryDto>> GetAll()
    {
        var orders = _store.GetAll();
        var dtos = _mapper.Map<IEnumerable<OrderSummaryDto>>(orders);
        return Ok(dtos);
    }

    /// <summary>
    /// Gets order details by ID mapped to OrderDetailDto with nested objects.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(OrderDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<OrderDetailDto> GetById(int id)
    {
        var order = _store.GetById(id);
        if (order == null)
        {
            return NotFound(new { message = $"Order with ID {id} not found." });
        }

        var dto = _mapper.Map<OrderDetailDto>(order);
        return Ok(dto);
    }

    /// <summary>
    /// Creates a new order by mapping CreateOrderRequest to Order entity.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(OrderDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<OrderDetailDto> Create([FromBody] CreateOrderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Customer?.FirstName) || string.IsNullOrWhiteSpace(request.Customer?.LastName))
        {
            return BadRequest(new { message = "Customer first and last name are required." });
        }

        if (request.Items == null || request.Items.Count == 0)
        {
            return BadRequest(new { message = "At least one order item is required." });
        }

        var order = _mapper.Map<Order>(request);
        var created = _store.Add(order);
        var resultDto = _mapper.Map<OrderDetailDto>(created);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, resultDto);
    }

    /// <summary>
    /// Updates shipping address by mapping UpdateAddressRequest onto existing Address entity.
    /// </summary>
    [HttpPut("{id:int}/address")]
    [ProducesResponseType(typeof(AddressDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<AddressDto> UpdateAddress(int id, [FromBody] UpdateAddressRequest request)
    {
        var order = _store.GetById(id);
        if (order == null)
        {
            return NotFound(new { message = $"Order with ID {id} not found." });
        }

        // Map request directly onto the existing object instance
        _mapper.Map(request, order.ShippingAddress);
        var dto = _mapper.Map<AddressDto>(order.ShippingAddress);

        return Ok(dto);
    }
}
