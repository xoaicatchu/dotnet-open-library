using ClosedXML.Excel;
using ExcelReportManager.Api.Models;

namespace ExcelReportManager.Api.Services;

public class ExcelReportService
{
    public byte[] ExportSalesReport(List<SaleRecordDto> records)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Sales Report");

        // Header
        worksheet.Cell(1, 1).Value = "Transaction ID";
        worksheet.Cell(1, 2).Value = "Customer Name";
        worksheet.Cell(1, 3).Value = "Product";
        worksheet.Cell(1, 4).Value = "Quantity";
        worksheet.Cell(1, 5).Value = "Unit Price";
        worksheet.Cell(1, 6).Value = "Total Amount";
        worksheet.Cell(1, 7).Value = "Sale Date";

        var headerRange = worksheet.Range(1, 1, 1, 7);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1B365D");
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Data Rows
        int currentRow = 2;
        foreach (var r in records)
        {
            worksheet.Cell(currentRow, 1).Value = r.TransactionId;
            worksheet.Cell(currentRow, 2).Value = r.CustomerName;
            worksheet.Cell(currentRow, 3).Value = r.Product;

            worksheet.Cell(currentRow, 4).Value = r.Quantity;
            worksheet.Cell(currentRow, 4).Style.NumberFormat.Format = "#,##0";

            worksheet.Cell(currentRow, 5).Value = r.UnitPrice;
            worksheet.Cell(currentRow, 5).Style.NumberFormat.Format = "$#,##0.00";

            // Total Amount formula: Quantity * UnitPrice
            worksheet.Cell(currentRow, 6).FormulaA1 = $"=D{currentRow}*E{currentRow}";
            worksheet.Cell(currentRow, 6).Style.NumberFormat.Format = "$#,##0.00";

            worksheet.Cell(currentRow, 7).Value = r.SaleDate.ToString("yyyy-MM-dd");
            worksheet.Cell(currentRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Zebra striping
            if (currentRow % 2 == 1)
            {
                worksheet.Range(currentRow, 1, currentRow, 7).Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F5F9");
            }

            currentRow++;
        }

        // Summary row (Total)
        if (records.Count > 0)
        {
            worksheet.Cell(currentRow, 3).Value = "TOTAL";
            worksheet.Cell(currentRow, 3).Style.Font.Bold = true;

            worksheet.Cell(currentRow, 4).FormulaA1 = $"=SUM(D2:D{currentRow - 1})";
            worksheet.Cell(currentRow, 4).Style.Font.Bold = true;
            worksheet.Cell(currentRow, 4).Style.NumberFormat.Format = "#,##0";

            worksheet.Cell(currentRow, 6).FormulaA1 = $"=SUM(F2:F{currentRow - 1})";
            worksheet.Cell(currentRow, 6).Style.Font.Bold = true;
            worksheet.Cell(currentRow, 6).Style.NumberFormat.Format = "$#,##0.00";

            var totalRange = worksheet.Range(currentRow, 1, currentRow, 7);
            totalRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
            totalRange.Style.Border.BottomBorder = XLBorderStyleValues.Double;
        }

        worksheet.Columns().AdjustToContents();

        using var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);
        return memoryStream.ToArray();
    }

    public ExcelImportResultDto ImportSalesReport(Stream stream)
    {
        var records = new List<SaleRecordDto>();
        var errors = new List<string>();

        try
        {
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
            {
                return new ExcelImportResultDto(0, 1, records, new List<string> { "Workbook has no worksheets" });
            }

            var rows = worksheet.RowsUsed().Skip(1); // Skip header

            int rowNumber = 1;
            foreach (var row in rows)
            {
                rowNumber++;

                // Skip summary/total row
                var col3Val = row.Cell(3).GetString().Trim();
                if (col3Val.Equals("TOTAL", StringComparison.OrdinalIgnoreCase))
                    continue;

                var txId = row.Cell(1).GetString().Trim();
                var customer = row.Cell(2).GetString().Trim();
                var product = row.Cell(3).GetString().Trim();

                if (string.IsNullOrWhiteSpace(txId) && string.IsNullOrWhiteSpace(customer))
                    continue;

                if (!row.Cell(4).TryGetValue<int>(out var quantity) || quantity <= 0)
                {
                    errors.Add($"Row {rowNumber}: Invalid quantity '{row.Cell(4).GetString()}'. Must be positive integer.");
                    continue;
                }

                if (!row.Cell(5).TryGetValue<decimal>(out var unitPrice) || unitPrice < 0)
                {
                    errors.Add($"Row {rowNumber}: Invalid unit price '{row.Cell(5).GetString()}'. Must be non-negative.");
                    continue;
                }

                DateTime saleDate;
                if (row.Cell(7).TryGetValue<DateTime>(out var parsedDate))
                {
                    saleDate = parsedDate;
                }
                else if (!DateTime.TryParse(row.Cell(7).GetString(), out saleDate))
                {
                    saleDate = DateTime.UtcNow;
                }

                records.Add(new SaleRecordDto(txId, customer, product, quantity, unitPrice, saleDate));
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to parse Excel file: {ex.Message}");
        }

        return new ExcelImportResultDto(records.Count, errors.Count, records, errors);
    }
}
