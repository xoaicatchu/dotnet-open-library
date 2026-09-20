using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using PdfReportGenerator.Api.Models;

namespace PdfReportGenerator.Api.Documents;

public class InvoiceDocument : IDocument
{
    private readonly InvoiceDto _model;

    public InvoiceDocument(InvoiceDto model)
    {
        _model = model;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Margin(40);
            page.Size(PageSizes.A4);
            page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Lato));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text(_model.SellerName).Bold().FontSize(18).FontColor(Colors.Blue.Darken2);
                col.Item().Text(_model.SellerAddress).FontColor(Colors.Grey.Medium);
            });

            row.RelativeItem().Column(col =>
            {
                col.Item().AlignRight().Text($"INVOICE #{_model.InvoiceNumber}").Bold().FontSize(16);
                col.Item().AlignRight().Text($"Issue Date: {_model.IssueDate:yyyy-MM-dd}").FontColor(Colors.Grey.Medium);
                col.Item().AlignRight().Text($"Due Date: {_model.DueDate:yyyy-MM-dd}").FontColor(Colors.Grey.Medium);
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingVertical(20).Column(col =>
        {
            // Bill To Section
            col.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(10).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("BILL TO:").Bold().FontSize(11).FontColor(Colors.Grey.Darken1);
                    c.Item().Text(_model.CustomerName).Bold();
                    c.Item().Text(_model.CustomerEmail).FontColor(Colors.Grey.Medium);
                    c.Item().Text(_model.CustomerAddress).FontColor(Colors.Grey.Medium);
                });
            });

            col.Item().PaddingTop(15);

            // Items Table
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);
                    columns.RelativeColumn(4);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("#").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Description").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Qty").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Unit Price").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Total").Bold();
                });

                // Data Rows
                for (int i = 0; i < _model.Items.Count; i++)
                {
                    var item = _model.Items[i];
                    var bg = i % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;

                    table.Cell().Background(bg).Padding(5).Text((i + 1).ToString());
                    table.Cell().Background(bg).Padding(5).Text(item.Description);
                    table.Cell().Background(bg).Padding(5).AlignRight().Text(item.Quantity.ToString());
                    table.Cell().Background(bg).Padding(5).AlignRight().Text($"{item.UnitPrice:N2}");
                    table.Cell().Background(bg).Padding(5).AlignRight().Text($"{item.Total:N2}");
                }
            });

            // Summary Section
            col.Item().PaddingTop(15).AlignRight().Width(200).Column(summary =>
            {
                summary.Item().Row(r =>
                {
                    r.RelativeItem().Text("Subtotal:");
                    r.RelativeItem().AlignRight().Text($"{_model.Subtotal:N2}");
                });

                summary.Item().Row(r =>
                {
                    r.RelativeItem().Text($"VAT ({_model.TaxRate:P0}):");
                    r.RelativeItem().AlignRight().Text($"{_model.TaxAmount:N2}");
                });

                summary.Item().BorderTop(1).BorderColor(Colors.Grey.Darken1).PaddingTop(5).Row(r =>
                {
                    r.RelativeItem().Text("Grand Total:").Bold().FontSize(12);
                    r.RelativeItem().AlignRight().Text($"{_model.GrandTotal:N2}").Bold().FontSize(12).FontColor(Colors.Blue.Darken2);
                });
            });

            // Notes
            if (!string.IsNullOrWhiteSpace(_model.Notes))
            {
                col.Item().PaddingTop(20).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(n =>
                {
                    n.Item().Text("Notes:").Bold();
                    n.Item().Text(_model.Notes).FontColor(Colors.Grey.Darken1);
                });
            }
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text("Thank you for your business!").FontColor(Colors.Grey.Medium);
            row.RelativeItem().AlignRight().Text(x =>
            {
                x.Span("Page ");
                x.CurrentPageNumber();
                x.Span(" of ");
                x.TotalPages();
            });
        });
    }
}
