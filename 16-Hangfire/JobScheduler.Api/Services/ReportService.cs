using JobScheduler.Api.Data;

namespace JobScheduler.Api.Services;

public class ReportService : IReportService
{
    private readonly JobAuditStore _auditStore;
    private readonly ILogger<ReportService> _logger;

    public ReportService(JobAuditStore auditStore, ILogger<ReportService> logger)
    {
        _auditStore = auditStore;
        _logger = logger;
    }

    public async Task GenerateDailyReport(string reportType)
    {
        _logger.LogInformation("Generating daily report: {ReportType}", reportType);
        await Task.Delay(100);
        _auditStore.RecordExecution("DailyReport", $"Generated {reportType} report");
    }
}
