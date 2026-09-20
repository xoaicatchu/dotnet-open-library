using System;

namespace OrderBilling.Api.Models;

public class Invoice
{
    public Guid OrderId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
