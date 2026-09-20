using AutoMapper;
using CsvHelper.Configuration;
using FluentValidation;
using MediatR;

namespace AllInOne.Api.Models;

public enum OrderStatus
{
    Draft,
    Submitted,
    Approved,
    Shipped,
    Completed,
    Cancelled
}

public enum OrderTrigger
{
    Submit,
    Approve,
    Ship,
    Complete,
    Cancel
}

public class OrderEntity
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string CustomerEmail { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<OrderItemEntity> Items { get; set; } = new();
}

public class OrderItemEntity
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice => Quantity * UnitPrice;
    public OrderEntity? Order { get; set; }
}

public record CreateOrderItemRequest(string ProductName, int Quantity, decimal UnitPrice);

public record CreateOrderRequest(
    string CustomerName,
    string CustomerEmail,
    List<CreateOrderItemRequest> Items);

public record OrderItemDto(int Id, string ProductName, int Quantity, decimal UnitPrice, decimal TotalPrice);

public record OrderDto(
    int Id,
    string OrderNumber,
    string CustomerName,
    string CustomerEmail,
    decimal TotalAmount,
    string Status,
    DateTime CreatedAt,
    List<OrderItemDto> Items);

public record OrderSummaryDto
{
    public int Id { get; init; }
    public string OrderNumber { get; init; } = "";
    public string CustomerName { get; init; } = "";
    public decimal TotalAmount { get; init; }
    public string Status { get; init; } = "";

    public OrderSummaryDto() { }

    public OrderSummaryDto(int id, string orderNumber, string customerName, decimal totalAmount, string status)
    {
        Id = id;
        OrderNumber = orderNumber;
        CustomerName = customerName;
        TotalAmount = totalAmount;
        Status = status;
    }
}

// Wolverine Command
public record CreateOrderCommand(CreateOrderRequest Request);

// MediatR Query
public record GetOrderByIdQuery(int Id) : IRequest<OrderDto?>;

// FluentValidation Validator
public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.CustomerName).NotEmpty().WithMessage("Customer name is required");
        RuleFor(x => x.CustomerEmail).NotEmpty().EmailAddress().WithMessage("Valid email is required");
        RuleFor(x => x.Items).NotEmpty().WithMessage("Order must contain at least one item");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductName).NotEmpty().WithMessage("Product name required");
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("Quantity must be > 0");
            item.RuleFor(i => i.UnitPrice).GreaterThan(0).WithMessage("Unit price must be > 0");
        });
    }
}

// AutoMapper Profile
public class OrderMappingProfile : Profile
{
    public OrderMappingProfile()
    {
        CreateMap<OrderEntity, OrderDto>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()));
        CreateMap<OrderItemEntity, OrderItemDto>();
    }
}

// CsvHelper ClassMap
public sealed class OrderCsvRecord
{
    public string OrderNumber { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string CustomerEmail { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public sealed class OrderCsvMap : ClassMap<OrderCsvRecord>
{
    public OrderCsvMap()
    {
        Map(m => m.OrderNumber).Name("Order Number");
        Map(m => m.CustomerName).Name("Customer Name");
        Map(m => m.CustomerEmail).Name("Email");
        Map(m => m.TotalAmount).Name("Total Amount").TypeConverterOption.Format("F2");
        Map(m => m.Status).Name("Status");
        Map(m => m.CreatedAt).Name("Created Date").TypeConverterOption.Format("yyyy-MM-dd");
    }
}
