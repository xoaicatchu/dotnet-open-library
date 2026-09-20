public interface IChatClient
{
    Task ReceiveMessage(string user, string message, DateTime sentAt);
    Task UserJoined(string user, string room);
    Task UserLeft(string user, string room);
    Task ReceiveNotification(string message, DateTime timestamp);
}
