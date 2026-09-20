using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using AllInOne.Api.Models;

namespace AllInOne.Api.Services;

public class OrderDocumentService
{
    static OrderDocumentService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.UseSystemFonts = false;
        QuestPDF.Settings.ThrowOnMissingFontFamilies = false;
    }

    // QuestPDF (Library 50) - Generate PDF Invoice
    public byte[] GenerateInvoicePdf(OrderDto order)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("ENTERPRISE PLATFORM").Bold().FontSize(18).FontColor(Colors.Blue.Darken2);
                        col.Item().Text("AllInOne Order Fulfillment System").FontColor(Colors.Grey.Medium);
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().AlignRight().Text($"INVOICE #{order.OrderNumber}").Bold().FontSize(14);
                        col.Item().AlignRight().Text($"Date: {order.CreatedAt:yyyy-MM-dd}").FontColor(Colors.Grey.Medium);
                        col.Item().AlignRight().Text($"Status: {order.Status}").FontColor(Colors.Green.Darken1);
                    });
                });

                page.Content().PaddingVertical(20).Column(col =>
                {
                    col.Item().Text($"Customer: {order.CustomerName} ({order.CustomerEmail})").Bold();
                    col.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(30);
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("#").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Product").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Qty").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Price").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Total").Bold();
                        });

                        for (int i = 0; i < order.Items.Count; i++)
                        {
                            var item = order.Items[i];
                            var bg = i % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;
                            table.Cell().Background(bg).Padding(5).Text((i + 1).ToString());
                            table.Cell().Background(bg).Padding(5).Text(item.ProductName);
                            table.Cell().Background(bg).Padding(5).AlignRight().Text(item.Quantity.ToString());
                            table.Cell().Background(bg).Padding(5).AlignRight().Text($"${item.UnitPrice:N2}");
                            table.Cell().Background(bg).Padding(5).AlignRight().Text($"${item.TotalPrice:N2}");
                        }
                    });

                    col.Item().PaddingTop(15).AlignRight().Text($"Grand Total: ${order.TotalAmount:N2}").Bold().FontSize(14).FontColor(Colors.Blue.Darken2);
                });

                page.Footer().Row(row =>
                {
                    row.RelativeItem().Text("Thank you for your order!").FontColor(Colors.Grey.Medium);
                    row.RelativeItem().AlignRight().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
                });
            });
        }).GeneratePdf();
    }

    // ClosedXML (Library 51) - Generate Styled Excel Spreadsheet
    public byte[] ExportOrdersExcel(List<OrderDto> orders)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Orders");

        worksheet.Cell(1, 1).Value = "Order Number";
        worksheet.Cell(1, 2).Value = "Customer Name";
        worksheet.Cell(1, 3).Value = "Email";
        worksheet.Cell(1, 4).Value = "Total Amount";
        worksheet.Cell(1, 5).Value = "Status";
        worksheet.Cell(1, 6).Value = "Created Date";

        var header = worksheet.Range(1, 1, 1, 6);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml("#1B365D");
        header.Style.Font.FontColor = XLColor.White;

        int row = 2;
        foreach (var o in orders)
        {
            worksheet.Cell(row, 1).Value = o.OrderNumber;
            worksheet.Cell(row, 2).Value = o.CustomerName;
            worksheet.Cell(row, 3).Value = o.CustomerEmail;
            worksheet.Cell(row, 4).Value = o.TotalAmount;
            worksheet.Cell(row, 4).Style.NumberFormat.Format = "$#,##0.00";
            worksheet.Cell(row, 5).Value = o.Status;
            worksheet.Cell(row, 6).Value = o.CreatedAt.ToString("yyyy-MM-dd");

            if (row % 2 == 1)
            {
                worksheet.Range(row, 1, row, 6).Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F5F9");
            }
            row++;
        }

        if (orders.Count > 0)
        {
            worksheet.Cell(row, 3).Value = "TOTAL";
            worksheet.Cell(row, 3).Style.Font.Bold = true;
            worksheet.Cell(row, 4).FormulaA1 = $"=SUM(D2:D{row - 1})";
            worksheet.Cell(row, 4).Style.Font.Bold = true;
            worksheet.Cell(row, 4).Style.NumberFormat.Format = "$#,##0.00";
        }

        worksheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    // CsvHelper (Library 52) - Export & Import CSV
    public byte[] ExportOrdersCsv(List<OrderDto> orders)
    {
        var records = orders.Select(o => new OrderCsvRecord
        {
            OrderNumber = o.OrderNumber,
            CustomerName = o.CustomerName,
            CustomerEmail = o.CustomerEmail,
            TotalAmount = o.TotalAmount,
            Status = o.Status,
            CreatedAt = o.CreatedAt
        }).ToList();

        using var ms = new MemoryStream();
        using var writer = new StreamWriter(ms, Encoding.UTF8);
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));

        csv.Context.RegisterClassMap<OrderCsvMap>();
        csv.WriteRecords(records);
        writer.Flush();

        return ms.ToArray();
    }

    public List<OrderCsvRecord> ImportOrdersCsv(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null
        });

        csv.Context.RegisterClassMap<OrderCsvMap>();
        return csv.GetRecords<OrderCsvRecord>().ToList();
    }
}
