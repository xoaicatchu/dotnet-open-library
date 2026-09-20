using System.Net;
using System.Net.Http.Json;
using CronScheduler.Api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CronScheduler.Tests;

public class QuartzTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public QuartzTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task MetricsCollector_ShouldCollectMetrics_Automatically()
    {
        var client = _factory.CreateClient();
        
        // Wait for the scheduled job to run at least once (runs every 2 seconds)
        await Task.Delay(2500);

        var metricsResponse = await client.GetAsync("/api/scheduler/metrics");
        metricsResponse.EnsureSuccessStatusCode();

        var metrics = await metricsResponse.Content.ReadFromJsonAsync<MetricRecord[]>();
        Assert.NotNull(metrics);
        Assert.NotEmpty(metrics);
    }

    [Fact]
    public async Task Backup_ShouldExecute_WhenTriggered()
    {
        var client = _factory.CreateClient();

        var request = new BackupRequest("Full");
        var response = await client.PostAsJsonAsync("/api/scheduler/backup/trigger", request);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        // Wait for the backup job to complete
        await Task.Delay(1500);

        var backupsResponse = await client.GetAsync("/api/scheduler/backups");
        backupsResponse.EnsureSuccessStatusCode();

        var backups = await backupsResponse.Content.ReadFromJsonAsync<BackupRecord[]>();
        Assert.NotNull(backups);
        Assert.Contains(backups, b => b.BackupType == "Full" && b.Status == "Success");
    }

    [Fact]
    public async Task Job_PauseAndResume_ShouldReturnOk()
    {
        var client = _factory.CreateClient();

        var pauseResponse = await client.PostAsync("/api/scheduler/jobs/metricCollector/pause", null);
        Assert.Equal(HttpStatusCode.OK, pauseResponse.StatusCode);

        var resumeResponse = await client.PostAsync("/api/scheduler/jobs/metricCollector/resume", null);
        Assert.Equal(HttpStatusCode.OK, resumeResponse.StatusCode);
    }

    [Fact]
    public async Task Swagger_IsAccessible()
    {
        var devClient = _factory.WithWebHostBuilder(builder => 
        {
            builder.UseEnvironment("Development");
        }).CreateClient();
        
        var response = await devClient.GetAsync("/swagger/index.html");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
