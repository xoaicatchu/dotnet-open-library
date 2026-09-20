using Polly;
using Polly.CircuitBreaker;
using Polly.Fallback;
using Polly.Retry;
using Polly.Timeout;
using ResilientPaymentGateway.Api.Models;
using ResilientPaymentGateway.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<IPaymentGatewayService, PaymentGatewayService>();

// Configure Polly v8 Resilience Pipeline: Fallback -> Retry -> Circuit Breaker -> Timeout
builder.Services.AddSingleton(sp =>
{
    return new ResiliencePipelineBuilder<PaymentResponse>()
        .AddFallback(new FallbackStrategyOptions<PaymentResponse>
        {
            FallbackAction = args => Outcome.FromResultAsValueTask(new PaymentResponse(
                TransactionId: Guid.NewGuid().ToString("N"),
                Status: "QueuedForOfflineProcessing",
                Attempts: 0,
                IsFallback: true,
                Message: "Primary payment gateway is currently unavailable. Payment queued safely."
            )),
            ShouldHandle = new PredicateBuilder<PaymentResponse>()
                .Handle<HttpRequestException>()
                .Handle<TimeoutRejectedException>()
                .Handle<BrokenCircuitException>()
        })
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions<PaymentResponse>
        {
            FailureRatio = 0.5,
            SamplingDuration = TimeSpan.FromSeconds(10),
            MinimumThroughput = 4,
            BreakDuration = TimeSpan.FromSeconds(1),
            ShouldHandle = new PredicateBuilder<PaymentResponse>()
                .Handle<HttpRequestException>()
                .Handle<TimeoutRejectedException>()
        })
        .AddRetry(new RetryStrategyOptions<PaymentResponse>
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(20),
            BackoffType = DelayBackoffType.Constant,
            ShouldHandle = new PredicateBuilder<PaymentResponse>()
                .Handle<HttpRequestException>()
                .Handle<TimeoutRejectedException>()
        })
        .AddTimeout(TimeSpan.FromMilliseconds(300))
        .Build();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();

public partial class Program;
