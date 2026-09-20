namespace PdfReportGenerator.Api.Models;

public record InvoiceItemDto(
    string Description,
    int Quantity,
    decimal UnitPrice)
{
    public decimal Total => Quantity * UnitPrice;
}

public record InvoiceDto(
    string InvoiceNumber,
    DateTime IssueDate,
    DateTime DueDate,
    string SellerName,
    string SellerAddress,
    string CustomerName,
    string CustomerEmail,
    string CustomerAddress,
    List<InvoiceItemDto> Items,
    string? Notes = null)
{
    public decimal Subtotal => Items.Sum(x => x.Total);
    public decimal TaxRate => 0.10m; // 10% VAT
    public decimal TaxAmount => Subtotal * TaxRate;
    public decimal GrandTotal => Subtotal + TaxAmount;
}

public record PdfMetadataDto(
    string InvoiceNumber,
    int PageCount,
    long FileSizeBytes,
    string ContentType);
