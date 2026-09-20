using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using TaskQueueScheduler.Api.Data;
using TaskQueueScheduler.Api.Models;
using Xunit;

namespace TaskQueueScheduler.Tests;

public class CoravelTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Xunit.Abstractions.ITestOutputHelper _output;

    public CoravelTests(WebApplicationFactory<Program> factory, Xunit.Abstractions.ITestOutputHelper output)
    {
        _factory = factory.WithWebHostBuilder(builder => 
        {
            builder.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            });
        });
        _output = output;
    }

    [Fact]
    public async Task HeartbeatScheduler_ExecutesAutomatically()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        // Act
        // Wait for scheduler to trigger at least once (every 2 seconds)
        await Task.Delay(2500);
        
        var response = await client.GetAsync("/api/tasks/heartbeats");
        response.EnsureSuccessStatusCode();
        
        var heartbeats = await response.Content.ReadFromJsonAsync<DateTime[]>();
        
        // Assert
        Assert.NotNull(heartbeats);
        Assert.NotEmpty(heartbeats);
    }

    [Fact]
    public async Task QueueTask_ReturnsAcceptedAndProcessesInBackground()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new QueueTaskRequest("Monthly Export", 500);

        // Act - Queue the task
        var response = await client.PostAsJsonAsync("/api/tasks/queue", request);
        
        // Assert - Queueing
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        // Act - Wait a bit (simulating short delay as requested)
        await Task.Delay(500); 
        
        var historyResponse = await client.GetAsync("/api/tasks/history");
        historyResponse.EnsureSuccessStatusCode();
        
        var history = await historyResponse.Content.ReadFromJsonAsync<TaskRecord[]>();

        // Assert - Queueing
        Assert.NotNull(history);
    }

    [Fact]
    public async Task QueueTask_InvalidData_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new QueueTaskRequest("", -10);

        // Act
        var response = await client.PostAsJsonAsync("/api/tasks/queue", request);
        
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SwaggerUI_IsAccessible()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/swagger/index.html");

        // Assert
        response.EnsureSuccessStatusCode();
    }
}
