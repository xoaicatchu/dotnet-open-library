namespace JobScheduler.Api.Services;

public interface IReportService
{
    Task GenerateDailyReport(string reportType);
}
