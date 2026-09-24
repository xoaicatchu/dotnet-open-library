using System.Net;
using System.Net.Http.Json;
using AiAgentMessaging.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AiAgentMessaging.Tests.Consumers;

public class OrderSubmittedConsumerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public OrderSubmittedConsumerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateOrder_ConsumerProcessesAndEnriches()
    {
        // Create order which triggers OrderSubmitted -> Consumer
        var request = new CreateOrderRequest("Alice", "Gaming Laptop", 1, 2500);
        var response = await _client.PostAsJsonAsync("/api/orders", request);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<Order>();
        Assert.NotNull(created);

        // Wait for MassTransit consumer to process
        await Task.Delay(500);

        // Verify order was processed with AI enrichment (NoOp since AI disabled by default)
        var getResponse = await _client.GetAsync($"/api/orders/{created.Id}");
        var order = await getResponse.Content.ReadFromJsonAsync<Order>();

        Assert.Equal("Processed", order!.Status);
        Assert.Equal("Unclassified", order.AiCategory);
        Assert.Equal("None", order.AiProvider);
    }

    [Fact]
    public async Task CreateOrder_NonExistingOrderGet_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/orders/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
