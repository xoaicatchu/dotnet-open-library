using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MiddlewareAdvanced.Api.ExceptionHandling;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    
    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }
    
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken ct)
    {
        _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        
        var (statusCode, title) = exception switch
        {
            BusinessException be => (StatusCodes.Status400BadRequest, be.Message),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            ArgumentException ae => (StatusCodes.Status400BadRequest, ae.Message),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };
        
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = $"https://httpstatuses.com/{statusCode}",
            Extensions =
            {
                ["correlationId"] = context.Items["CorrelationId"],
                ["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier
            }
        };
        
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problem, problem.GetType(), new System.Text.Json.JsonSerializerOptions(), "application/problem+json", ct);
        return true;
    }
}

public class BusinessException : Exception
{
    public BusinessException(string message) : base(message) { }
}
