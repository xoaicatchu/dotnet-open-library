using System;
using System.ComponentModel.DataAnnotations;

namespace SagaPattern.Api.Models;

public record OrderDto(Guid OrderId, string CustomerName, decimal TotalAmount);

public class PlaceOrderRequest
{
    [Required]
    public string CustomerName { get; set; } = string.Empty;
    
    [Range(0.01, 10000)]
    public decimal TotalAmount { get; set; }
}
