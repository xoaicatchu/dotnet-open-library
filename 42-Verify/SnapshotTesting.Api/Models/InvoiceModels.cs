namespace SnapshotTesting.Api.Models;

public record CustomerInfo(
    Guid Id,
    string Name,
    string TaxCode,
    string Email,
    string Address
);

public record InvoiceLineItem(
    Guid ItemId,
    string Description,
    decimal UnitPrice,
    int Quantity,
    decimal Discount,
    decimal LineTotal
);

public record InvoiceDetail(
    Guid Id,
    string InvoiceNumber,
    DateTime IssueDate,
    DateTime DueDate,
    CustomerInfo Customer,
    List<InvoiceLineItem> Items,
    decimal SubTotal,
    decimal TaxRate,
    decimal TaxAmount,
    decimal DiscountTotal,
    decimal GrandTotal,
    string Notes,
    string Status
);

public record CreateInvoiceItemRequest(
    string Description,
    decimal UnitPrice,
    int Quantity,
    decimal Discount = 0
);

public record CreateInvoiceRequest(
    CustomerInfo Customer,
    List<CreateInvoiceItemRequest> Items,
    decimal TaxRate = 0.1m,
    string Notes = ""
);

public record InvoiceSummary(
    Guid Id,
    string InvoiceNumber,
    string CustomerName,
    DateTime IssueDate,
    decimal GrandTotal,
    string Status
);
