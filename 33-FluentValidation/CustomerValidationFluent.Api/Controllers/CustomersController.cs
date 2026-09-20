using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using CustomerValidationFluent.Api.Models;

namespace CustomerValidationFluent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly IValidator<CustomerRegistrationRequest> _registrationValidator;
    private readonly IValidator<CreateOrderRequest> _orderValidator;
    private static readonly List<CustomerResponse> Customers = [];
    private static int _nextId = 1;

    public CustomersController(
        IValidator<CustomerRegistrationRequest> registrationValidator,
        IValidator<CreateOrderRequest> orderValidator)
    {
        _registrationValidator = registrationValidator;
        _orderValidator = orderValidator;
    }

    /// <summary>
    /// Registers a new customer after executing FluentValidation rules.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] CustomerRegistrationRequest request)
    {
        var validationResult = await _registrationValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var modelState = new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary();
            foreach (var error in validationResult.Errors)
            {
                modelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            return ValidationProblem(modelState);
        }

        var customer = new CustomerResponse(
            Id: _nextId++,
            FullName: request.FullName,
            Email: request.Email,
            Age: request.Age,
            Address: request.Address,
            IsVip: request.IsVip,
            MembershipNumber: request.MembershipNumber,
            CreatedAt: DateTime.UtcNow
        );

        Customers.Add(customer);
        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer);
    }

    /// <summary>
    /// Gets a customer by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetById(int id)
    {
        var customer = Customers.FirstOrDefault(c => c.Id == id);
        if (customer == null)
        {
            return NotFound(new { message = $"Customer with ID {id} was not found." });
        }
        return Ok(customer);
    }

    /// <summary>
    /// Validates and submits an order with nested line items and business rules.
    /// </summary>
    [HttpPost("orders")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var validationResult = await _orderValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var modelState = new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary();
            foreach (var error in validationResult.Errors)
            {
                modelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            return ValidationProblem(modelState);
        }

        decimal subtotal = request.Items.Sum(item => item.Quantity * item.UnitPrice);
        decimal discount = subtotal * (request.DiscountPercent / 100m);
        decimal total = subtotal - discount;

        var order = new OrderResponse(
            OrderId: $"ORD-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}",
            CustomerEmail: request.CustomerEmail,
            Items: request.Items,
            TotalAmount: total,
            ShippingMethod: request.ShippingMethod,
            CreatedAt: DateTime.UtcNow
        );

        return Ok(order);
    }
}
