using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using StateMachineDemo.Api.Models;
using Xunit;

namespace StateMachineDemo.Tests;

public class OrderStateMachineTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public OrderStateMachineTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateOrder_InitialStateIsDraft()
    {
        var request = new CreateOrderRequest("Customer 1", 100m);
        var response = await _client.PostAsJsonAsync("/api/orders", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(order);
        Assert.Equal(OrderStatus.Draft, order.Status);
        Assert.Contains(OrderTrigger.Submit, order.PermittedTriggers);
        Assert.Contains(OrderTrigger.Cancel, order.PermittedTriggers);
        Assert.DoesNotContain(OrderTrigger.Approve, order.PermittedTriggers);
    }

    [Fact]
    public async Task CreateOrder_ValidationFailsOnEmptyName()
    {
        var request = new CreateOrderRequest("", 100m);
        var response = await _client.PostAsJsonAsync("/api/orders", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_ValidationFailsOnNegativeAmount()
    {
        var request = new CreateOrderRequest("Customer 1", -10m);
        var response = await _client.PostAsJsonAsync("/api/orders", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Transition_DraftToSubmitted_Succeeds()
    {
        // 1. Create order
        var createResponse = await _client.PostAsJsonAsync("/api/orders", new CreateOrderRequest("Customer 2", 200m));
        var order = await createResponse.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(order);

        // 2. Submit
        var fireResponse = await _client.PostAsJsonAsync($"/api/orders/{order.Id}/fire", new FireTriggerRequest(OrderTrigger.Submit, "Submitted by user"));
        Assert.Equal(HttpStatusCode.OK, fireResponse.StatusCode);

        var updatedOrder = await fireResponse.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(updatedOrder);
        Assert.Equal(OrderStatus.Submitted, updatedOrder.Status);
        Assert.Contains(OrderTrigger.StartReview, updatedOrder.PermittedTriggers);
        Assert.Single(updatedOrder.History);
        Assert.Equal(OrderStatus.Draft, updatedOrder.History[0].FromStatus);
        Assert.Equal(OrderStatus.Submitted, updatedOrder.History[0].ToStatus);
    }

    [Fact]
    public async Task Transition_InvalidTrigger_ReturnsBadRequest()
    {
        // 1. Create order in Draft
        var createResponse = await _client.PostAsJsonAsync("/api/orders", new CreateOrderRequest("Customer 3", 300m));
        var order = await createResponse.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(order);

        // 2. Try to directly Approve a Draft order (invalid transition)
        var fireResponse = await _client.PostAsJsonAsync($"/api/orders/{order.Id}/fire", new FireTriggerRequest(OrderTrigger.Approve));
        Assert.Equal(HttpStatusCode.BadRequest, fireResponse.StatusCode);
    }

    [Fact]
    public async Task FullLifecycle_DraftToApproved()
    {
        // 1. Create
        var create = await _client.PostAsJsonAsync("/api/orders", new CreateOrderRequest("Customer 4", 400m));
        var order = await create.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(order);

        // 2. Submit (Draft -> Submitted)
        await _client.PostAsJsonAsync($"/api/orders/{order.Id}/fire", new FireTriggerRequest(OrderTrigger.Submit));

        // 3. StartReview (Submitted -> UnderReview)
        await _client.PostAsJsonAsync($"/api/orders/{order.Id}/fire", new FireTriggerRequest(OrderTrigger.StartReview));

        // 4. Approve (UnderReview -> Approved)
        var approve = await _client.PostAsJsonAsync($"/api/orders/{order.Id}/fire", new FireTriggerRequest(OrderTrigger.Approve, "Manager approved"));
        Assert.Equal(HttpStatusCode.OK, approve.StatusCode);

        var finalOrder = await approve.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(finalOrder);
        Assert.Equal(OrderStatus.Approved, finalOrder.Status);
        Assert.Equal(3, finalOrder.History.Count);
    }

    [Fact]
    public async Task GetGraph_ReturnsValidDiagram()
    {
        var create = await _client.PostAsJsonAsync("/api/orders", new CreateOrderRequest("Customer 5", 500m));
        var order = await create.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(order);

        // DOT format
        var dotResponse = await _client.GetAsync($"/api/orders/{order.Id}/graph?format=dot");
        Assert.Equal(HttpStatusCode.OK, dotResponse.StatusCode);
        var dotContent = await dotResponse.Content.ReadAsStringAsync();
        Assert.Contains("digraph", dotContent);

        // Mermaid format
        var mermaidResponse = await _client.GetAsync($"/api/orders/{order.Id}/graph?format=mermaid");
        Assert.Equal(HttpStatusCode.OK, mermaidResponse.StatusCode);
        var mermaidContent = await mermaidResponse.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrWhiteSpace(mermaidContent));
    }

    [Fact]
    public async Task GetOrder_NotFound_Returns404()
    {
        var response = await _client.GetAsync("/api/orders/non-existent-id");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
