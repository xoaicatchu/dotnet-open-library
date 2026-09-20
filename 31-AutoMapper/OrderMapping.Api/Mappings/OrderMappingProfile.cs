using AutoMapper;
using OrderMapping.Api.Entities;
using OrderMapping.Api.Models;

namespace OrderMapping.Api.Mappings;

public class OrderMappingProfile : Profile
{
    public OrderMappingProfile()
    {
        // Customer mappings
        CreateMap<Customer, CustomerDto>()
            .ForMember(d => d.FullName, opt => opt.MapFrom(s => $"{s.FirstName} {s.LastName}".Trim()));

        CreateMap<CreateCustomerRequest, Customer>()
            .ForMember(d => d.Id, opt => opt.Ignore());

        // Address mappings
        CreateMap<Address, AddressDto>().ReverseMap();
        CreateMap<UpdateAddressRequest, Address>();

        // OrderItem mappings
        CreateMap<OrderItem, OrderItemDto>()
            .ForMember(d => d.LineTotal, opt => opt.MapFrom(s => s.UnitPrice * s.Quantity));

        CreateMap<CreateOrderItemRequest, OrderItem>()
            .ForMember(d => d.Id, opt => opt.Ignore());

        // Order mappings
        CreateMap<CreateOrderRequest, Order>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.OrderNumber, opt => opt.Ignore())
            .ForMember(d => d.BillingAddress, opt => opt.Ignore())
            .ForMember(d => d.Status, opt => opt.Ignore())
            .ForMember(d => d.CreatedAt, opt => opt.Ignore());

        CreateMap<Order, OrderSummaryDto>()
            .ForMember(d => d.CustomerFullName, opt => opt.MapFrom(s => $"{s.Customer.FirstName} {s.Customer.LastName}".Trim()))
            .ForMember(d => d.TotalAmount, opt => opt.MapFrom(s => s.Items.Sum(i => i.UnitPrice * i.Quantity)))
            .ForMember(d => d.ItemsCount, opt => opt.MapFrom(s => s.Items.Sum(i => i.Quantity)));

        CreateMap<Order, OrderDetailDto>()
            .ForMember(d => d.TotalAmount, opt => opt.MapFrom(s => s.Items.Sum(i => i.UnitPrice * i.Quantity)));
    }
}
