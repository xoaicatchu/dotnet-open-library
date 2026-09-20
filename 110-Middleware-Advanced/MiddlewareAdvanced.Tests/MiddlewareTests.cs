using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MiddlewareAdvanced.Tests;

public class MiddlewareTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public MiddlewareTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Test1_GetProducts_Returns200_And_HasCorrelationId()
    {
        var response = await _client.GetAsync("/api/products");
        response.EnsureSuccessStatusCode();
        Assert.True(response.Headers.Contains("X-Correlation-Id"));
    }

    [Fact]
    public async Task Test2_GetProducts_WithCorrelationId_ReturnsSameCorrelationId()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/products");
        var expectedId = Guid.NewGuid().ToString();
        request.Headers.Add("X-Correlation-Id", expectedId);
        
        var response = await _client.SendAsync(request);
        
        response.EnsureSuccessStatusCode();
        var actualId = response.Headers.GetValues("X-Correlation-Id").First();
        Assert.Equal(expectedId, actualId);
    }

    [Fact]
    public async Task Test3_GetDemoCorrelationId_ReturnsCorrectBody()
    {
        var expectedId = Guid.NewGuid().ToString();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/demo/correlation-id");
        request.Headers.Add("X-Correlation-Id", expectedId);
        
        var response = await _client.SendAsync(request);
        
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(expectedId, body.GetProperty("correlationId").GetString());
    }

    [Fact]
    public async Task Test4_GetDemoNotFound_Returns404_ProblemDetails()
    {
        var response = await _client.GetAsync("/api/demo/not-found");
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        
        Assert.Equal(404, body.GetProperty("status").GetInt32());
        Assert.Equal("Resource not found", body.GetProperty("title").GetString());
        Assert.Equal("https://httpstatuses.com/404", body.GetProperty("type").GetString());
    }

    [Fact]
    public async Task Test5_GetDemoBusinessError_Returns400_ProblemDetails()
    {
        var response = await _client.GetAsync("/api/demo/business-error");
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        
        Assert.Equal(400, body.GetProperty("status").GetInt32());
        Assert.Equal("Insufficient inventory", body.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Test6_GetDemoServerError_Returns500_ProblemDetails_NoStackTrace()
    {
        var response = await _client.GetAsync("/api/demo/server-error");
        
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var contentString = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("StackTrace", contentString, StringComparison.OrdinalIgnoreCase);
        
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(500, body.GetProperty("status").GetInt32());
        Assert.Equal("An unexpected error occurred", body.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Test7_GetPing_ReturnsPong_MapWhen()
    {
        var response = await _client.GetAsync("/ping");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("pong", content);
    }

    [Fact]
    public async Task Test8_PostProducts_Valid_Returns201()
    {
        var response = await _client.PostAsJsonAsync("/api/products", new { name = "Product B" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Test9_PostProducts_Invalid_Returns400_ProblemDetailsWithCorrelationId()
    {
        var response = await _client.PostAsJsonAsync("/api/products", new { name = "" });
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        
        Assert.True(body.TryGetProperty("correlationId", out _));
    }

    [Fact]
    public async Task Test10_GetDemoSlowEndpoint_Returns200()
    {
        var response = await _client.GetAsync("/api/demo/slow-endpoint");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        
        Assert.Equal("Slow response", body.GetProperty("message").GetString());
        Assert.Equal(200, body.GetProperty("timeMs").GetInt32());
    }

    [Fact]
    public async Task Test11_ProblemDetails_ContainsTypeStatusTitleCorrelationId()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/demo/not-found");
        var id = Guid.NewGuid().ToString();
        request.Headers.Add("X-Correlation-Id", id);
        
        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        
        Assert.True(body.TryGetProperty("type", out _));
        Assert.True(body.TryGetProperty("status", out _));
        Assert.True(body.TryGetProperty("title", out _));
        Assert.True(body.TryGetProperty("correlationId", out var cId));
        Assert.Equal(id, cId.GetString());
    }

    [Fact]
    public async Task Test12_ErrorResponse_ContentType_IsProblemJson()
    {
        var response = await _client.GetAsync("/api/demo/server-error");
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
