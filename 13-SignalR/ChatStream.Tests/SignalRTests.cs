using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Xunit;

public class SignalRTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SignalRTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HubConnection_ConnectsSuccessfully()
    {
        var server = _factory.Server;
        var hubConnection = new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/chat", options =>
            {
                options.HttpMessageHandlerFactory = _ => server.CreateHandler();
            })
            .Build();

        await hubConnection.StartAsync();

        Assert.Equal(HubConnectionState.Connected, hubConnection.State);

        await hubConnection.StopAsync();
    }

    [Fact]
    public async Task BroadcastMessage_ClientReceivesMessage()
    {
        var server = _factory.Server;
        var hubConnection = new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/chat", options =>
            {
                options.HttpMessageHandlerFactory = _ => server.CreateHandler();
            })
            .Build();

        string receivedUser = null;
        string receivedMessage = null;

        var tcs = new TaskCompletionSource<bool>();

        hubConnection.On<string, string, DateTime>("ReceiveMessage", (user, message, sentAt) =>
        {
            receivedUser = user;
            receivedMessage = message;
            tcs.SetResult(true);
        });

        await hubConnection.StartAsync();

        await hubConnection.InvokeAsync("BroadcastMessage", "TestUser", "Hello World");

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(2000));
        Assert.Equal(tcs.Task, completed);
        Assert.Equal("TestUser", receivedUser);
        Assert.Equal("Hello World", receivedMessage);

        await hubConnection.StopAsync();
    }

    [Fact]
    public async Task RoomIsolation_ClientInRoomReceivesMessage()
    {
        var server = _factory.Server;
        var connection1 = new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/chat", options =>
            {
                options.HttpMessageHandlerFactory = _ => server.CreateHandler();
            })
            .Build();

        var connection2 = new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/chat", options =>
            {
                options.HttpMessageHandlerFactory = _ => server.CreateHandler();
            })
            .Build();

        var tcs1 = new TaskCompletionSource<bool>();
        var tcs2 = new TaskCompletionSource<bool>();

        connection1.On<string, string, DateTime>("ReceiveMessage", (u, m, d) => tcs1.TrySetResult(true));
        connection2.On<string, string, DateTime>("ReceiveMessage", (u, m, d) => tcs2.TrySetResult(true));

        await connection1.StartAsync();
        await connection2.StartAsync();

        await connection1.InvokeAsync("JoinRoom", "User1", "RoomA");
        // connection2 is not in RoomA

        await connection1.InvokeAsync("SendMessageToRoom", "User1", "RoomA", "Room message");

        var completed1 = await Task.WhenAny(tcs1.Task, Task.Delay(2000));
        Assert.Equal(tcs1.Task, completed1);

        var completed2 = await Task.WhenAny(tcs2.Task, Task.Delay(500));
        Assert.NotEqual(tcs2.Task, completed2); // Should not receive

        await connection1.StopAsync();
        await connection2.StopAsync();
    }

    [Fact]
    public async Task HttpBroadcast_InvokesReceiveNotification()
    {
        var server = _factory.Server;
        var client = _factory.CreateClient();

        var hubConnection = new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/chat", options =>
            {
                options.HttpMessageHandlerFactory = _ => server.CreateHandler();
            })
            .Build();

        string receivedNotification = null;
        var tcs = new TaskCompletionSource<bool>();

        hubConnection.On<string, DateTime>("ReceiveNotification", (message, timestamp) =>
        {
            receivedNotification = message;
            tcs.TrySetResult(true);
        });

        await hubConnection.StartAsync();

        var request = new { Message = "System maintenance in 10 minutes" };
        var response = await client.PostAsJsonAsync("/api/notifications/broadcast", request);
        response.EnsureSuccessStatusCode();

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(2000));
        Assert.Equal(tcs.Task, completed);
        Assert.Equal("System maintenance in 10 minutes", receivedNotification);

        await hubConnection.StopAsync();
    }

    [Fact]
    public async Task SwaggerEndpoint_Returns200OK()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/openapi/v1.json");
        response.EnsureSuccessStatusCode();
    }
}
