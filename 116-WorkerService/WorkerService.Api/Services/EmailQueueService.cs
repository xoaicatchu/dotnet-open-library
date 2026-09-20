using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace WorkerService.Api.Services;

public class EmailQueueService : IEmailQueue
{
    private readonly Channel<EmailMessage> _channel;
    
    public EmailQueueService()
    {
        _channel = Channel.CreateBounded<EmailMessage>(new BoundedChannelOptions(100) {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,   // single consumer
            SingleWriter = false   // multiple producers OK
        });
    }
    
    public ValueTask EnqueueAsync(EmailMessage email, CancellationToken ct = default)
        => _channel.Writer.WriteAsync(email, ct);
    
    public async IAsyncEnumerable<EmailMessage> DequeueAllAsync([EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var item in _channel.Reader.ReadAllAsync(ct))
            yield return item;
    }
    
    public int Count => _channel.Reader.Count;
}
