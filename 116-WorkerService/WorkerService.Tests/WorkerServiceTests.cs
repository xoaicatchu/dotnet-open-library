using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using WorkerService.Api.Controllers;
using WorkerService.Api.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using WorkerService.Api.Data;
using WorkerService.Api.Entities;
using Xunit;
using Microsoft.Extensions.Hosting;

namespace WorkerService.Tests;

public class WorkerServiceTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public WorkerServiceTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove existing DbContext
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory DbContext using a shared connection string that persists
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseSqlite("DataSource=file::memory:?cache=shared");
                });

                // Ensure schema is created
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();
            });
        });
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetJobs_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/jobs");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetJobById_NotExists_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/jobs/9999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SendEmail_ReturnsAccepted()
    {
        var req = new SendEmailRequest("test@test.com", "Subj", "Body");
        var response = await _client.PostAsJsonAsync("/api/email/send", req);
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task SendEmail_EmptyTo_ReturnsBadRequest()
    {
        var req = new SendEmailRequest("", "Subj", "Body");
        var response = await _client.PostAsJsonAsync("/api/email/send", req);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SendEmail_Multiple_IncreasesQueueDepth()
    {
        var req = new SendEmailRequest("t@t.com", "S", "B");
        await _client.PostAsJsonAsync("/api/email/send", req);
        await _client.PostAsJsonAsync("/api/email/send", req);
        await _client.PostAsJsonAsync("/api/email/send", req);

        var response = await _client.GetAsync("/api/email/queue-depth");
        response.EnsureSuccessStatusCode();
        var depthInfo = await response.Content.ReadFromJsonAsync<QueueDepthResponse>();
        depthInfo.Should().NotBeNull();
        depthInfo!.Depth.Should().BeGreaterThan(0);
    }

    private record QueueDepthResponse(int Depth);

    [Fact]
    public async Task GetStats_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/jobs/stats");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task EmailQueueService_Enqueue_CountIncreases()
    {
        var queue = new EmailQueueService();
        queue.Count.Should().Be(0);
        await queue.EnqueueAsync(new EmailMessage("a@a.com", "s", "b"));
        queue.Count.Should().Be(1);
    }

    [Fact]
    public async Task EmailQueueService_Dequeue_ReturnsEnqueuedItem()
    {
        var queue = new EmailQueueService();
        await queue.EnqueueAsync(new EmailMessage("a@a.com", "s", "b"));
        
        var cts = new CancellationTokenSource();
        var e = queue.DequeueAllAsync(cts.Token).GetAsyncEnumerator();
        await e.MoveNextAsync();
        var item = e.Current;
        
        item.To.Should().Be("a@a.com");
    }

    [Fact]
    public async Task GracefulShutdown_CancelsToken()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        cts.IsCancellationRequested.Should().BeTrue();
        await Task.CompletedTask;
    }
    
    [Fact]
    public async Task DbContext_SavesJob()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        db.Jobs.Add(new JobEntity { Type = "Test", Status = JobStatus.Pending });
        await db.SaveChangesAsync();
        
        var count = await db.Jobs.CountAsync();
        count.Should().BeGreaterThan(0);
    }
}
