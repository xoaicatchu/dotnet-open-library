using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SmartHomeHub.Api.Models;

namespace SmartHomeHub.Tests;

public class SmartHomeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SmartHomeTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostCommand_UpdatesDeviceState_And_GetRetrievesIt()
    {
        // Arrange
        var client = _factory.CreateClient();
        var deviceId = "test-light-1";
        var command = new DeviceCommand
        {
            DeviceType = "Light",
            Command = "{\"power\":\"ON\"}"
        };

        // Act 1: Send command
        var postResponse = await client.PostAsJsonAsync($"/api/devices/{deviceId}/command", command);
        
        // Assert 1
        Assert.Equal(HttpStatusCode.Accepted, postResponse.StatusCode);

        // Wait for the simulated device to report its state
        await Task.Delay(1000);

        // Act 2: Get device state
        var getResponse = await client.GetAsync($"/api/devices/{deviceId}");

        // Assert 2
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var state = await getResponse.Content.ReadFromJsonAsync<DeviceState>();
        Assert.NotNull(state);
        Assert.Equal(deviceId, state.DeviceId);
        Assert.Equal("Light", state.DeviceType);
        Assert.Equal("{\"power\":\"ON\"}", state.Payload);

        // Act 3: Get all devices
        var getAllResponse = await client.GetAsync("/api/devices");
        Assert.Equal(HttpStatusCode.OK, getAllResponse.StatusCode);
        var allDevices = await getAllResponse.Content.ReadFromJsonAsync<DeviceState[]>();
        Assert.NotNull(allDevices);
        Assert.Contains(allDevices, d => d.DeviceId == deviceId);
    }

    [Fact]
    public async Task Get_NonExistentDevice_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/devices/non-existent");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_EmptyDeviceId_ReturnsBadRequestOrNotFound()
    {
        var client = _factory.CreateClient();
        var command = new DeviceCommand { DeviceType = "Light", Command = "ON" };
        
        // This will result in 405 Method Not Allowed or 404 Not Found because /api/devices//command is not a valid route
        var response = await client.PostAsJsonAsync($"/api/devices/ /command", command);
        
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task Get_SwaggerUI_ReturnsSuccess()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/swagger/index.html");
        Assert.True(response.IsSuccessStatusCode);
    }
}
