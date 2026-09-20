using Grpc.Core;
using gRPCStreaming.Shared;

namespace gRPCStreaming.Server.Services;

public class ChatStreamService : Chat.ChatBase
{
    // Bidirectional: echo với timestamp + prefix
    public override async Task Connect(
        IAsyncStreamReader<ChatMessage> requestStream,
        IServerStreamWriter<ChatMessage> responseStream,
        ServerCallContext context)
    {
        await foreach (var msg in requestStream.ReadAllAsync(context.CancellationToken))
        {
            // Echo lại với server timestamp
            await responseStream.WriteAsync(new ChatMessage {
                User = "Server",
                Message = $"[Echo] {msg.User}: {msg.Message}",
                Timestamp = DateTime.UtcNow.ToString("O")
            });
        }
    }
}
