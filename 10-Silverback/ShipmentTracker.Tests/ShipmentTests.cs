using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using ShipmentTracker.Api.Controllers;
using ShipmentTracker.Api.Models;
using Xunit;

namespace ShipmentTracker.Tests;

public class ShipmentTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ShipmentTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateShipment_ReturnsCreatedAndLocation()
    {
        var client = _factory.CreateClient();
        var request = new CreateShipmentRequest("123 Test St");

        var response = await client.PostAsJsonAsync("/api/shipments", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var shipment = await response.Content.ReadFromJsonAsync<Shipment>();
        Assert.NotNull(shipment);
        Assert.Equal("123 Test St", shipment.Destination);
        Assert.Equal("Created", shipment.Status);
        Assert.NotNull(response.Headers.Location);
        Assert.Equal($"/api/shipments/{shipment.Id}", response.Headers.Location.ToString());
    }

    [Fact]
    public async Task CreateShipment_EmptyDestination_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var request = new CreateShipmentRequest("");

        var response = await client.PostAsJsonAsync("/api/shipments", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetShipmentById_ReturnsCorrectData()
    {
        var client = _factory.CreateClient();
        
        // Create first
        var createResponse = await client.PostAsJsonAsync("/api/shipments", new CreateShipmentRequest("456 Get St"));
        var created = await createResponse.Content.ReadFromJsonAsync<Shipment>();
        Assert.NotNull(created);

        // Get
        var response = await client.GetAsync($"/api/shipments/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var fetched = await response.Content.ReadFromJsonAsync<Shipment>();
        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched.Id);
    }

    [Fact]
    public async Task GetShipmentById_NonExistent_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/shipments/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ListShipments_ReturnsCreatedShipments()
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/shipments", new CreateShipmentRequest("List St"));

        var response = await client.GetAsync("/api/shipments");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<Shipment>>();
        Assert.NotNull(list);
        Assert.NotEmpty(list);
    }

    [Fact]
    public async Task UpdateStatus_ReturnsOkAndStatusUpdated()
    {
        var client = _factory.CreateClient();
        
        var createResponse = await client.PostAsJsonAsync("/api/shipments", new CreateShipmentRequest("Update St"));
        var created = await createResponse.Content.ReadFromJsonAsync<Shipment>();
        Assert.NotNull(created);

        var updateReq = new UpdateStatusRequest("InTransit");
        var updateResponse = await client.PutAsJsonAsync($"/api/shipments/{created.Id}/status", updateReq);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<Shipment>();
        Assert.NotNull(updated);
        Assert.Equal("InTransit", updated.Status);
    }

    [Fact]
    public async Task UpdateStatus_InvalidStatus_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        
        var createResponse = await client.PostAsJsonAsync("/api/shipments", new CreateShipmentRequest("Update St"));
        var created = await createResponse.Content.ReadFromJsonAsync<Shipment>();
        Assert.NotNull(created);

        var updateReq = new UpdateStatusRequest("InvalidStatusXYZ");
        var updateResponse = await client.PutAsJsonAsync($"/api/shipments/{created.Id}/status", updateReq);

        Assert.Equal(HttpStatusCode.BadRequest, updateResponse.StatusCode);
    }

    [Fact]
    public async Task SwaggerUI_IsAvailable()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/swagger/index.html");
        Assert.True(response.IsSuccessStatusCode);
    }
}
