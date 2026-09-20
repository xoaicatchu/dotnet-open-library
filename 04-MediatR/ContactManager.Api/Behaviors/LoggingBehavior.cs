using System.Diagnostics;
using MediatR;

namespace ContactManager.Api.Behaviors;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("Handling {RequestName}", requestName);
        var timer = Stopwatch.StartNew();
        
        var response = await next();
        
        timer.Stop();
        _logger.LogInformation("Handled {RequestName} in {ElapsedMilliseconds} ms", requestName, timer.ElapsedMilliseconds);
        
        return response;
    }
}
