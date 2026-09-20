namespace ExcelReportManager.Api.Models;

public record SaleRecordDto(
    string TransactionId,
    string CustomerName,
    string Product,
    int Quantity,
    decimal UnitPrice,
    DateTime SaleDate)
{
    public decimal TotalAmount => Quantity * UnitPrice;
}

public record SaleSummaryDto(
    int TotalTransactions,
    int TotalQuantity,
    decimal TotalRevenue);

public record ExcelImportResultDto(
    int SuccessCount,
    int ErrorCount,
    List<SaleRecordDto> Records,
    List<string> Errors);
