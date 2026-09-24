using System.Net;
using System.Net.Http.Json;
using AiAgentMessaging.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AiAgentMessaging.Tests.Controllers;

public class OrdersControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public OrdersControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateOrder_ValidRequest_ReturnsAccepted()
    {
        var request = new CreateOrderRequest("John Doe", "Laptop", 1, 1000);
        var response = await _client.PostAsJsonAsync("/api/orders", request);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var order = await response.Content.ReadFromJsonAsync<Order>();
        Assert.NotNull(order);
        Assert.Equal("Submitted", order.Status);
        Assert.Equal("John Doe", order.CustomerName);
    }

    [Fact]
    public async Task CreateOrder_EmptyName_ReturnsBadRequest()
    {
        var request = new CreateOrderRequest("", "Laptop", 1, 1000);
        var response = await _client.PostAsJsonAsync("/api/orders", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_ZeroQuantity_ReturnsBadRequest()
    {
        var request = new CreateOrderRequest("Test", "Widget", 0, 100);
        var response = await _client.PostAsJsonAsync("/api/orders", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_NegativePrice_ReturnsBadRequest()
    {
        var request = new CreateOrderRequest("Test", "Widget", 1, -10);
        var response = await _client.PostAsJsonAsync("/api/orders", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/orders");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ExistingOrder_ReturnsOk()
    {
        var request = new CreateOrderRequest("Jane", "Phone", 2, 500);
        var postResponse = await _client.PostAsJsonAsync("/api/orders", request);
        var created = await postResponse.Content.ReadFromJsonAsync<Order>();

        var getResponse = await _client.GetAsync($"/api/orders/{created!.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task GetById_NonExisting_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/orders/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task OrderStatus_BecomesProcessed_AfterDelay()
    {
        var request = new CreateOrderRequest("Tom", "Mouse", 1, 50);
        var postResponse = await _client.PostAsJsonAsync("/api/orders", request);
        var created = await postResponse.Content.ReadFromJsonAsync<Order>();

        // Wait for MassTransit to process
        await Task.Delay(500);

        var getResponse = await _client.GetAsync($"/api/orders/{created!.Id}");
        var order = await getResponse.Content.ReadFromJsonAsync<Order>();
        Assert.Equal("Processed", order!.Status);
    }

    [Fact]
    public async Task Swagger_IsAvailable()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        Assert.True(response.IsSuccessStatusCode);
    }
}
