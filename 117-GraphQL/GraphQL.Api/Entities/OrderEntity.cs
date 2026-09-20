using System;
using System.ComponentModel.DataAnnotations;

namespace GraphQL.Api.Entities;

public class OrderEntity
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ProductEntity Product { get; set; } = null!;
}
