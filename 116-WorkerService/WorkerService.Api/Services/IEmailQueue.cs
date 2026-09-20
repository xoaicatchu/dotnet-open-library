using System.Runtime.CompilerServices;

namespace WorkerService.Api.Services;

public record EmailMessage(string To, string Subject, string Body);

public interface IEmailQueue
{
    ValueTask EnqueueAsync(EmailMessage email, CancellationToken ct = default);
    IAsyncEnumerable<EmailMessage> DequeueAllAsync(CancellationToken ct);
    int Count { get; }
}
