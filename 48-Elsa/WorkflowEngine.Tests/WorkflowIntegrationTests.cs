using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using WorkflowEngine.Api.Models;
using Xunit;

namespace WorkflowEngine.Tests;

public class WorkflowIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public WorkflowIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetDefinitions_ReturnsAvailableWorkflows()
    {
        var response = await _client.GetAsync("/api/workflows/definitions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var definitions = await response.Content.ReadFromJsonAsync<List<WorkflowDefinitionDto>>();
        Assert.NotNull(definitions);
        Assert.Equal(2, definitions.Count);
        Assert.Contains(definitions, d => d.Name == "GreetingWorkflow");
        Assert.Contains(definitions, d => d.Name == "OrderApprovalWorkflow");
    }

    [Fact]
    public async Task RunGreeting_ReturnsCompletedStatus()
    {
        var response = await _client.PostAsync("/api/workflows/run/greeting?name=TestUser", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<RunWorkflowResponse>();
        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result.WorkflowInstanceId));
        Assert.Equal("Finished", result.Status);
    }

    [Fact]
    public async Task RunGreeting_DefaultName_ReturnsCompletedStatus()
    {
        var response = await _client.PostAsync("/api/workflows/run/greeting", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<RunWorkflowResponse>();
        Assert.NotNull(result);
        Assert.Equal("Finished", result.Status);
    }

    [Fact]
    public async Task RunOrderApproval_SmallAmount_AutoApproved()
    {
        var input = new OrderApprovalInput("ORD-TEST-001", 500m, "tester");
        var response = await _client.PostAsJsonAsync("/api/workflows/run/order-approval", input);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<RunWorkflowResponse>();
        Assert.NotNull(result);
        Assert.Equal("Finished", result.Status);
    }

    [Fact]
    public async Task RunOrderApproval_LargeAmount_RequiresReview()
    {
        var input = new OrderApprovalInput("ORD-TEST-002", 5000m, "tester");
        var response = await _client.PostAsJsonAsync("/api/workflows/run/order-approval", input);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<RunWorkflowResponse>();
        Assert.NotNull(result);
        Assert.Equal("Finished", result.Status);
    }

    [Fact]
    public async Task RunOrderApproval_EmptyOrderId_ReturnsBadRequest()
    {
        var input = new OrderApprovalInput("", 500m, "tester");
        var response = await _client.PostAsJsonAsync("/api/workflows/run/order-approval", input);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RunOrderApproval_NegativeAmount_ReturnsBadRequest()
    {
        var input = new OrderApprovalInput("ORD-001", -100m, "tester");
        var response = await _client.PostAsJsonAsync("/api/workflows/run/order-approval", input);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SwaggerEndpoint_IsAccessible()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
