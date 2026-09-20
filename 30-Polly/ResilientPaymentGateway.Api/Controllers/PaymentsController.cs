using Microsoft.AspNetCore.Mvc;
using Polly;
using ResilientPaymentGateway.Api.Models;
using ResilientPaymentGateway.Api.Services;

namespace ResilientPaymentGateway.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly ResiliencePipeline<PaymentResponse> _pipeline;
    private readonly IPaymentGatewayService _gatewayService;

    public PaymentsController(
        ResiliencePipeline<PaymentResponse> pipeline,
        IPaymentGatewayService gatewayService)
    {
        _pipeline = pipeline;
        _gatewayService = gatewayService;
    }

    /// <summary>
    /// Processes a payment through a Polly v8 Resilience Pipeline:
    /// (Fallback -> Retry -> Circuit Breaker -> Timeout).
    /// </summary>
    [HttpPost("process")]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaymentResponse>> ProcessPayment([FromBody] PaymentRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.AccountId))
        {
            return BadRequest(new { message = "AccountId is required." });
        }

        if (request.Amount <= 0)
        {
            return BadRequest(new { message = "Amount must be greater than 0." });
        }

        // Execute payment logic through the Polly Resilience Pipeline
        var response = await _pipeline.ExecuteAsync(
            async token => await _gatewayService.ProcessPaymentAsync(request, token),
            cancellationToken
        );

        return Ok(response);
    }

    /// <summary>
    /// Configures the simulated gateway behavior: Success, TransientFailure, PermanentFailure, Timeout.
    /// </summary>
    [HttpPost("gateway-behavior")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult SetGatewayBehavior([FromQuery] GatewayBehavior behavior)
    {
        _gatewayService.SetBehavior(behavior);
        return Ok(new { message = $"Gateway behavior updated to: {behavior}" });
    }

    /// <summary>
    /// Gets gateway simulation metrics and total attempt counter.
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(GatewayStatusDto), StatusCodes.Status200OK)]
    public ActionResult<GatewayStatusDto> GetStatus()
    {
        return Ok(new GatewayStatusDto(_gatewayService.TotalAttempts, _gatewayService.CurrentBehavior));
    }

    /// <summary>
    /// Resets the gateway service attempts and behavior to default.
    /// </summary>
    [HttpPost("reset")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Reset()
    {
        _gatewayService.Reset();
        return Ok(new { message = "Gateway service reset to initial state." });
    }
}
