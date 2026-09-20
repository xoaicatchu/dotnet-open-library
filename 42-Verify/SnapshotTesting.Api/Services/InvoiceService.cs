using System.Collections.Concurrent;
using SnapshotTesting.Api.Models;

namespace SnapshotTesting.Api.Services;

public interface IInvoiceService
{
    InvoiceDetail? GetInvoiceById(Guid id);
    InvoiceDetail CreateInvoice(CreateInvoiceRequest request);
    InvoiceSummary? GetInvoiceSummary(Guid id);
    List<InvoiceSummary> GetAllInvoices();
}

public class InvoiceService : IInvoiceService
{
    private readonly ConcurrentDictionary<Guid, InvoiceDetail> _invoices = new();

    public InvoiceService()
    {
        SeedSampleInvoices();
    }

    private void SeedSampleInvoices()
    {
        var id1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var invoice1 = new InvoiceDetail(
            Id: id1,
            InvoiceNumber: "INV-2026-0001",
            IssueDate: new DateTime(2026, 1, 15, 9, 0, 0, DateTimeKind.Utc),
            DueDate: new DateTime(2026, 2, 15, 9, 0, 0, DateTimeKind.Utc),
            Customer: new CustomerInfo(
                Id: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                Name: "Acme Corporation",
                TaxCode: "0101234567",
                Email: "billing@acme.com",
                Address: "123 Tech Park, Innovation Way, Silicon City"
            ),
            Items:
            [
                new InvoiceLineItem(
                    ItemId: Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    Description: "Enterprise Cloud Hosting - Annual Plan",
                    UnitPrice: 1200.00m,
                    Quantity: 1,
                    Discount: 100.00m,
                    LineTotal: 1100.00m
                ),
                new InvoiceLineItem(
                    ItemId: Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                    Description: "Premium Support & SLA 24/7",
                    UnitPrice: 300.00m,
                    Quantity: 2,
                    Discount: 0.00m,
                    LineTotal: 600.00m
                )
            ],
            SubTotal: 1700.00m,
            TaxRate: 0.10m,
            TaxAmount: 170.00m,
            DiscountTotal: 100.00m,
            GrandTotal: 1870.00m,
            Notes: "Thank you for your business. Payment is due within 30 days.",
            Status: "Issued"
        );

        _invoices[id1] = invoice1;
    }

    public InvoiceDetail? GetInvoiceById(Guid id)
    {
        _invoices.TryGetValue(id, out var invoice);
        return invoice;
    }

    public InvoiceDetail CreateInvoice(CreateInvoiceRequest request)
    {
        var id = Guid.NewGuid();
        var issueDate = DateTime.UtcNow;
        var dueDate = issueDate.AddDays(30);

        var lineItems = new List<InvoiceLineItem>();
        decimal subTotal = 0;
        decimal discountTotal = 0;

        foreach (var item in request.Items)
        {
            var lineTotal = (item.UnitPrice * item.Quantity) - item.Discount;
            subTotal += (item.UnitPrice * item.Quantity);
            discountTotal += item.Discount;

            lineItems.Add(new InvoiceLineItem(
                ItemId: Guid.NewGuid(),
                Description: item.Description,
                UnitPrice: item.UnitPrice,
                Quantity: item.Quantity,
                Discount: item.Discount,
                LineTotal: lineTotal
            ));
        }

        var taxableAmount = subTotal - discountTotal;
        var taxAmount = Math.Round(taxableAmount * request.TaxRate, 2);
        var grandTotal = taxableAmount + taxAmount;

        var invoice = new InvoiceDetail(
            Id: id,
            InvoiceNumber: $"INV-{issueDate:yyyyMM}-{id.ToString()[..8].ToUpper()}",
            IssueDate: issueDate,
            DueDate: dueDate,
            Customer: request.Customer,
            Items: lineItems,
            SubTotal: subTotal,
            TaxRate: request.TaxRate,
            TaxAmount: taxAmount,
            DiscountTotal: discountTotal,
            GrandTotal: grandTotal,
            Notes: request.Notes,
            Status: "Draft"
        );

        _invoices[id] = invoice;
        return invoice;
    }

    public InvoiceSummary? GetInvoiceSummary(Guid id)
    {
        if (_invoices.TryGetValue(id, out var invoice))
        {
            return new InvoiceSummary(
                invoice.Id,
                invoice.InvoiceNumber,
                invoice.Customer.Name,
                invoice.IssueDate,
                invoice.GrandTotal,
                invoice.Status
            );
        }

        return null;
    }

    public List<InvoiceSummary> GetAllInvoices()
    {
        return _invoices.Values
            .Select(i => new InvoiceSummary(i.Id, i.InvoiceNumber, i.Customer.Name, i.IssueDate, i.GrandTotal, i.Status))
            .ToList();
    }
}
