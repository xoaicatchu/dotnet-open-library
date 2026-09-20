using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Api.Abstractions;
using NotificationService.Api.Decorators;
using NotificationService.Api.Controllers;
using Xunit;

namespace NotificationService.Tests;

public class ScrutorTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ScrutorTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public void AssemblyScanning_RegistersAllSenders()
    {
        using var scope = _factory.Services.CreateScope();
        var senders = scope.ServiceProvider.GetRequiredService<IEnumerable<INotificationSender>>().ToList();
        
        Assert.Equal(3, senders.Count);
        Assert.Contains(senders, s => s.Channel == "email");
        Assert.Contains(senders, s => s.Channel == "sms");
        Assert.Contains(senders, s => s.Channel == "push");
    }

    [Fact]
    public void AssemblyScanning_RegistersAllFormatters()
    {
        using var scope = _factory.Services.CreateScope();
        var formatters = scope.ServiceProvider.GetRequiredService<IEnumerable<IMessageFormatter>>().ToList();
        
        Assert.Equal(2, formatters.Count);
        Assert.Contains(formatters, f => f.Format == "plain");
        Assert.Contains(formatters, f => f.Format == "html");
    }

    [Fact]
    public void Decoration_WrapsServices_InCorrectOrder()
    {
        using var scope = _factory.Services.CreateScope();
        var senders = scope.ServiceProvider.GetRequiredService<IEnumerable<INotificationSender>>().ToList();
        
        // Outermost should be RetryNotificationSender
        foreach (var sender in senders)
        {
            Assert.IsType<RetryNotificationSender>(sender);
            
            // To test inner, we would need to use reflection since it's private.
            var innerField = typeof(RetryNotificationSender).GetField("_inner", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var inner = innerField!.GetValue(sender);
            
            Assert.IsType<LoggingNotificationSender>(inner);
        }
    }

    [Fact]
    public async Task GetChannels_ReturnsThreeChannels()
    {
        var client = _factory.CreateClient();
        var channels = await client.GetFromJsonAsync<string[]>("/api/notifications/channels");
        
        Assert.NotNull(channels);
        Assert.Equal(3, channels.Length);
        Assert.Contains("email", channels);
        Assert.Contains("sms", channels);
        Assert.Contains("push", channels);
    }

    [Fact]
    public async Task GetFormatters_ReturnsTwoFormatters()
    {
        var client = _factory.CreateClient();
        var formatters = await client.GetFromJsonAsync<string[]>("/api/notifications/formatters");
        
        Assert.NotNull(formatters);
        Assert.Equal(2, formatters.Length);
        Assert.Contains("plain", formatters);
        Assert.Contains("html", formatters);
    }

    [Fact]
    public async Task PostSend_ReturnsSuccess_ForValidRequest()
    {
        var client = _factory.CreateClient();
        var request = new SendNotificationRequest("email", "test@test.com", "Test", "Body");
        
        var response = await client.PostAsJsonAsync("/api/notifications/send", request);
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<NotificationResult>();
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("email", result.Channel);
    }

    [Fact]
    public async Task PostSend_ReturnsBadRequest_ForUnknownChannel()
    {
        var client = _factory.CreateClient();
        var request = new SendNotificationRequest("unknown", "test@test.com", "Test", "Body");
        
        var response = await client.PostAsJsonAsync("/api/notifications/send", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostBroadcast_ReturnsResultsForAllChannels()
    {
        var client = _factory.CreateClient();
        var request = new BroadcastNotificationRequest("test@test.com", "Test", "Body");
        
        var response = await client.PostAsJsonAsync("/api/notifications/broadcast", request);
        response.EnsureSuccessStatusCode();
        
        var results = await response.Content.ReadFromJsonAsync<NotificationResult[]>();
        Assert.NotNull(results);
        Assert.Equal(3, results.Length);
    }

    [Fact]
    public async Task Swagger_IsAvailable()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();
    }
}
