using System.Net;
using System.Net.Http.Json;
using JobScheduler.Api.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace JobScheduler.Tests;

public class HangfireTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HangfireTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task EnqueueWelcomeEmail_ReturnsAcceptedAndProcessesJob()
    {
        // Act - Enqueue
        var response = await _client.PostAsJsonAsync("/api/jobs/welcome-email", new { email = "test@test.com", name = "Test User" });

        // Assert - Enqueue response
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JobResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.JobId);

        // Act - Wait for processing
        await Task.Delay(2000); // Wait for worker to pick up and process

        // Assert - Check history
        var historyResponse = await _client.GetAsync("/api/jobs/history");
        historyResponse.EnsureSuccessStatusCode();
        var history = await historyResponse.Content.ReadFromJsonAsync<List<JobExecutionRecord>>();
        
        Assert.NotNull(history);
        Assert.Contains(history, h => h.JobType == "WelcomeEmail" && h.Details.Contains("test@test.com"));
    }

    [Fact]
    public async Task TriggerRecurringReport_ReturnsOkAndProcessesJob()
    {
        // Act - Trigger
        var response = await _client.PostAsJsonAsync("/api/jobs/recurring/daily-report", new { reportType = "TestReport" });

        // Assert - Trigger response
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Act - Wait for processing
        await Task.Delay(2000);

        // Assert - Check history
        var historyResponse = await _client.GetAsync("/api/jobs/history");
        historyResponse.EnsureSuccessStatusCode();
        var history = await historyResponse.Content.ReadFromJsonAsync<List<JobExecutionRecord>>();
        
        Assert.NotNull(history);
        Assert.Contains(history, h => h.JobType == "DailyReport" && h.Details.Contains("TestReport"));
    }

    [Fact]
    public async Task WelcomeEmail_EmptyEmail_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/jobs/welcome-email", new { email = "", name = "Test User" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task HangfireDashboard_ReturnsSuccess()
    {
        var response = await _client.GetAsync("/hangfire");
        // Often it might redirect or just return OK if authenticated
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Found || response.StatusCode == HttpStatusCode.MovedPermanently);
    }

    [Fact]
    public async Task SwaggerUI_ReturnsSuccess()
    {
        var response = await _client.GetAsync("/swagger/index.html");
        Assert.True(response.IsSuccessStatusCode);
    }

    private class JobResponse
    {
        public string? JobId { get; set; }
        public string? Type { get; set; }
    }
}
