using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using NLog;
using NLog.Targets;
using AuditLogNLog.Api.Models;

namespace AuditLogNLog.Tests;

public class NLogTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly MemoryTarget _memoryTarget;

    public NLogTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();

        _memoryTarget = new MemoryTarget("memTarget")
        {
            Layout = "${level:uppercase=true}|${logger}|${message}"
        };

        var config = LogManager.Configuration ?? new NLog.Config.LoggingConfiguration();
        config.AddTarget(_memoryTarget);
        config.AddRuleForAllLevels(_memoryTarget);
        LogManager.Configuration = config;
        LogManager.ReconfigExistingLoggers();
    }

    [Fact]
    public async Task Login_Success_LogsInfoWithUserAndIp()
    {
        _memoryTarget.Logs.Clear();

        var request = new LoginAuditRequest("john_doe", "10.0.0.1", true);
        var response = await _client.PostAsJsonAsync("/api/audit/login", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var log = _memoryTarget.Logs.FirstOrDefault(l => l.Contains("logged in successfully"));
        Assert.NotNull(log);
        Assert.Contains("INFO|", log);
        Assert.Contains("john_doe", log);
        Assert.Contains("10.0.0.1", log);
    }

    [Fact]
    public async Task Login_Failure_LogsWarning()
    {
        _memoryTarget.Logs.Clear();

        var request = new LoginAuditRequest("bad_actor", "192.168.1.50", false);
        var response = await _client.PostAsJsonAsync("/api/audit/login", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var log = _memoryTarget.Logs.FirstOrDefault(l => l.Contains("Failed login attempt"));
        Assert.NotNull(log);
        Assert.Contains("WARN|", log);
        Assert.Contains("bad_actor", log);
    }

    [Fact]
    public async Task Action_AdminAction_LogsInfoWithAuditDetails()
    {
        _memoryTarget.Logs.Clear();

        var request = new ActionAuditRequest("admin_sarah", "RESET_PASSWORD", "USR-101", "Admin reset user password");
        var response = await _client.PostAsJsonAsync("/api/audit/action", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var log = _memoryTarget.Logs.FirstOrDefault(l => l.Contains("RESET_PASSWORD"));
        Assert.NotNull(log);
        Assert.Contains("INFO|", log);
        Assert.Contains("admin_sarah", log);
        Assert.Contains("USR-101", log);
    }

    [Fact]
    public async Task Health_LogsAuditHeartbeat()
    {
        _memoryTarget.Logs.Clear();

        var response = await _client.GetAsync("/api/audit/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var log = _memoryTarget.Logs.FirstOrDefault(l => l.Contains("health check executed successfully"));
        Assert.NotNull(log);
        Assert.Contains("INFO|", log);
    }

    [Fact]
    public async Task Login_EmptyUsername_ReturnsBadRequest()
    {
        var request = new LoginAuditRequest("", "10.0.0.1", true);
        var response = await _client.PostAsJsonAsync("/api/audit/login", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
