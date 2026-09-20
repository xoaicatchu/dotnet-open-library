using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using OrderProcessor.Api.Models;
using OrderProcessor.Api.Controllers;

namespace OrderProcessor.Tests;

public class OrderProcessingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public OrderProcessingTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SubmitOrder_ReturnsAccepted_AndOrderWithStatus()
    {
        var request = new CreateOrderRequest("John Doe", "Laptop", 1, 1000m);
        var response = await _client.PostAsJsonAsync("/api/orders", request);
        
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<Order>();
        Assert.NotNull(order);
        Assert.Equal("Submitted", order.Status);
    }

    [Fact]
    public async Task GetOrderById_AfterSubmit_ReturnsOrder()
    {
        var request = new CreateOrderRequest("Jane Doe", "Phone", 2, 500m);
        var postResponse = await _client.PostAsJsonAsync("/api/orders", request);
        var createdOrder = await postResponse.Content.ReadFromJsonAsync<Order>();

        var getResponse = await _client.GetAsync($"/api/orders/{createdOrder!.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        
        var fetchedOrder = await getResponse.Content.ReadFromJsonAsync<Order>();
        Assert.Equal(createdOrder.Id, fetchedOrder!.Id);
    }

    [Fact]
    public async Task ListOrders_ReturnsAllSubmitted()
    {
        var request = new CreateOrderRequest("Bob", "Tablet", 1, 300m);
        await _client.PostAsJsonAsync("/api/orders", request);

        var getResponse = await _client.GetAsync("/api/orders");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        
        var orders = await getResponse.Content.ReadFromJsonAsync<List<Order>>();
        Assert.NotNull(orders);
        Assert.NotEmpty(orders);
    }

    [Fact]
    public async Task CancelOrder_ChangesStatus()
    {
        var request = new CreateOrderRequest("Alice", "Monitor", 1, 200m);
        var postResponse = await _client.PostAsJsonAsync("/api/orders", request);
        var createdOrder = await postResponse.Content.ReadFromJsonAsync<Order>();

        // Give it a chance to start processing
        await Task.Delay(200);

        var cancelResponse = await _client.PostAsync($"/api/orders/{createdOrder!.Id}/cancel", null);
        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);

        // Wait a little for MassTransit to process the event
        await Task.Delay(500);

        var getResponse = await _client.GetAsync($"/api/orders/{createdOrder.Id}");
        var updatedOrder = await getResponse.Content.ReadFromJsonAsync<Order>();
        
        Assert.Equal("Cancelled", updatedOrder!.Status);
    }

    [Fact]
    public async Task InvalidOrder_ReturnsBadRequest()
    {
        var request = new CreateOrderRequest("", "Laptop", 1, 1000m); // Empty name
        var response = await _client.PostAsJsonAsync("/api/orders", request);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task NonExistentOrder_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/orders/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task OrderStatus_BecomesCompleted_AfterDelay()
    {
        var request = new CreateOrderRequest("Tom", "Mouse", 1, 50m);
        var postResponse = await _client.PostAsJsonAsync("/api/orders", request);
        var createdOrder = await postResponse.Content.ReadFromJsonAsync<Order>();

        // Wait for MassTransit to process the event and the consumer to finish its simulated delay
        await Task.Delay(500);

        var getResponse = await _client.GetAsync($"/api/orders/{createdOrder!.Id}");
        var updatedOrder = await getResponse.Content.ReadFromJsonAsync<Order>();
        
        Assert.Equal("Completed", updatedOrder!.Status);
    }
    
    [Fact]
    public async Task Swagger_IsAvailable()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        Assert.True(response.IsSuccessStatusCode);
    }
}
