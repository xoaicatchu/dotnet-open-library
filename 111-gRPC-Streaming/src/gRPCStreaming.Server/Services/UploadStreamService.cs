using Grpc.Core;
using gRPCStreaming.Shared;

namespace gRPCStreaming.Server.Services;

public class UploadStreamService : BulkUpload.BulkUploadBase
{
    // Client Streaming: đọc từng record từ client, trả về summary
    public override async Task<UploadSummary> UploadProducts(
        IAsyncStreamReader<ProductUploadRequest> requestStream,
        ServerCallContext context)
    {
        var received = 0;
        var saved = new List<string>();
        
        await foreach (var request in requestStream.ReadAllAsync(context.CancellationToken))
        {
            received++;
            if (!string.IsNullOrEmpty(request.Name) && request.Price > 0)
                saved.Add(request.Name);
        }
        
        return new UploadSummary {
            TotalReceived = received,
            TotalSaved = saved.Count,
            Message = $"Upload complete. Saved: {string.Join(", ", saved.Take(3))}"
        };
    }
}
