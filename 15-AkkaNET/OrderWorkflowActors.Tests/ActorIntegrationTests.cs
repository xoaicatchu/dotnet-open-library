using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using OrderWorkflowActors.Api.Messages;
using OrderWorkflowActors.Api.Controllers;
using Xunit;

namespace OrderWorkflowActors.Tests;

public class ActorIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ActorIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateOrder_Returns201CreatedAndLocationHeader()
    {
        var client = _factory.CreateClient();
        var req = new CreateOrderRequest("Alice", 1200000m);
        var response = await client.PostAsJsonAsync("/api/actor-orders", req);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task GetOrderStatus_Returns200WithPendingStatus()
    {
        var client = _factory.CreateClient();
        var createResponse = await client.PostAsJsonAsync("/api/actor-orders", new CreateOrderRequest("Bob", 500m));
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<OrderCreated>();
        
        var getResponse = await client.GetAsync($"/api/actor-orders/{createdOrder!.OrderId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        
        var details = await getResponse.Content.ReadFromJsonAsync<OrderDetails>();
        Assert.Equal("Pending", details!.Status);
        Assert.Equal("Bob", details.CustomerName);
    }

    [Fact]
    public async Task PayOrder_SuccessfullyTransitionsStatusToPaid()
    {
        var client = _factory.CreateClient();
        var createResponse = await client.PostAsJsonAsync("/api/actor-orders", new CreateOrderRequest("Charlie", 1000m));
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<OrderCreated>();
        
        var payResponse = await client.PostAsJsonAsync($"/api/actor-orders/{createdOrder!.OrderId}/pay", new ProcessPaymentRequest(1000m));
        Assert.Equal(HttpStatusCode.OK, payResponse.StatusCode);
        
        var details = await payResponse.Content.ReadFromJsonAsync<OrderDetails>();
        Assert.Equal("Paid", details!.Status);
    }

    [Fact]
    public async Task CancelOrder_SuccessfullyTransitionsStatusToCancelled()
    {
        var client = _factory.CreateClient();
        var createResponse = await client.PostAsJsonAsync("/api/actor-orders", new CreateOrderRequest("Dave", 1000m));
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<OrderCreated>();
        
        var cancelResponse = await client.PostAsync($"/api/actor-orders/{createdOrder!.OrderId}/cancel", null);
        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);
        
        var details = await cancelResponse.Content.ReadFromJsonAsync<OrderDetails>();
        Assert.Equal("Cancelled", details!.Status);
    }

    [Fact]
    public async Task PayingAlreadyPaidOrder_Returns400BadRequest()
    {
        var client = _factory.CreateClient();
        var createResponse = await client.PostAsJsonAsync("/api/actor-orders", new CreateOrderRequest("Eve", 1000m));
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<OrderCreated>();
        
        await client.PostAsJsonAsync($"/api/actor-orders/{createdOrder!.OrderId}/pay", new ProcessPaymentRequest(1000m));
        var secondPayResponse = await client.PostAsJsonAsync($"/api/actor-orders/{createdOrder!.OrderId}/pay", new ProcessPaymentRequest(1000m));
        
        Assert.Equal(HttpStatusCode.BadRequest, secondPayResponse.StatusCode);
    }

    [Fact]
    public async Task CancellingAlreadyPaidOrder_Returns400BadRequest()
    {
        var client = _factory.CreateClient();
        var createResponse = await client.PostAsJsonAsync("/api/actor-orders", new CreateOrderRequest("Frank", 1000m));
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<OrderCreated>();
        
        await client.PostAsJsonAsync($"/api/actor-orders/{createdOrder!.OrderId}/pay", new ProcessPaymentRequest(1000m));
        var cancelResponse = await client.PostAsync($"/api/actor-orders/{createdOrder!.OrderId}/cancel", null);
        
        Assert.Equal(HttpStatusCode.BadRequest, cancelResponse.StatusCode);
    }

    [Fact]
    public async Task Validation_EmptyCustomerNameOrNegativeAmount_Returns400()
    {
        var client = _factory.CreateClient();
        
        var req1 = new CreateOrderRequest("", 1000m);
        var response1 = await client.PostAsJsonAsync("/api/actor-orders", req1);
        Assert.Equal(HttpStatusCode.BadRequest, response1.StatusCode);
        
        var req2 = new CreateOrderRequest("Grace", -10m);
        var response2 = await client.PostAsJsonAsync("/api/actor-orders", req2);
        Assert.Equal(HttpStatusCode.BadRequest, response2.StatusCode);
    }

    [Fact]
    public async Task NonExistentOrder_Returns404()
    {
        var client = _factory.CreateClient();
        var id = Guid.NewGuid();
        
        var getResponse = await client.GetAsync($"/api/actor-orders/{id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        
        var payResponse = await client.PostAsJsonAsync($"/api/actor-orders/{id}/pay", new ProcessPaymentRequest(1000m));
        Assert.Equal(HttpStatusCode.NotFound, payResponse.StatusCode);
    }

    [Fact]
    public async Task SwaggerUIEndpoint_IsAccessible()
    {
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Environment", "Development");
        });
        var client = factory.CreateClient();
        var response = await client.GetAsync("/swagger/index.html");
        response.EnsureSuccessStatusCode();
    }
}
