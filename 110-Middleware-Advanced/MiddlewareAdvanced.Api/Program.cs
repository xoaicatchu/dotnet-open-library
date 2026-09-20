using MiddlewareAdvanced.Api.ExceptionHandling;
using MiddlewareAdvanced.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ProblemDetails RFC 9457 (built-in .NET 8+)
builder.Services.AddProblemDetails();

// IExceptionHandler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Register middlewares as scoped (IMiddleware)
builder.Services.AddScoped<CorrelationIdMiddleware>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Middleware pipeline order matters:
app.UseExceptionHandler(); // ProblemDetails + GlobalExceptionHandler
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestResponseLoggingMiddleware>();

// Map conditional: /health goes to different path
app.MapWhen(ctx => ctx.Request.Path.StartsWithSegments("/ping"),
    appBuilder => appBuilder.Run(ctx => ctx.Response.WriteAsync("pong")));

if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.MapControllers();
app.Run();

public partial class Program { }
