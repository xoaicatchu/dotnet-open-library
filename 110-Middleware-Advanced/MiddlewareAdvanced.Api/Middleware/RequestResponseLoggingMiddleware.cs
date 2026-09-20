using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace MiddlewareAdvanced.Api.Middleware;

public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
    
    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        var correlationId = context.Items["CorrelationId"] as string ?? "N/A";
        
        // Log request
        _logger.LogInformation(
            "[{CorrelationId}] → {Method} {Path} {QueryString}",
            correlationId,
            context.Request.Method,
            context.Request.Path,
            context.Request.QueryString);
        
        await _next(context);
        
        sw.Stop();
        // Log response
        _logger.LogInformation(
            "[{CorrelationId}] ← {StatusCode} in {ElapsedMs}ms",
            correlationId,
            context.Response.StatusCode,
            sw.ElapsedMilliseconds);
    }
}
