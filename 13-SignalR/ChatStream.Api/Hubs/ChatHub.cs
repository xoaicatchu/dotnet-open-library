using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

public class ChatHub : Hub<IChatClient>
{
    public async Task JoinRoom(string user, string room)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, room);
        await Clients.Group(room).UserJoined(user, room);
    }

    public async Task LeaveRoom(string user, string room)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, room);
        await Clients.Group(room).UserLeft(user, room);
    }

    public async Task SendMessageToRoom(string user, string room, string message)
    {
        await Clients.Group(room).ReceiveMessage(user, message, DateTime.UtcNow);
    }

    public async Task BroadcastMessage(string user, string message)
    {
        await Clients.All.ReceiveMessage(user, message, DateTime.UtcNow);
    }
}
