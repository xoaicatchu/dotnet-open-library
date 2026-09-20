using Flurl.Http;
using Microsoft.AspNetCore.Mvc;
using PaymentGatewayFlurl.Api.Models;
using PaymentGatewayFlurl.Api.Services;

namespace PaymentGatewayFlurl.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentGatewayController : ControllerBase
{
    private readonly IPaymentGatewayService _gatewayService;
    private readonly ILogger<PaymentGatewayController> _logger;

    public PaymentGatewayController(IPaymentGatewayService gatewayService, ILogger<PaymentGatewayController> logger)
    {
        _gatewayService = gatewayService;
        _logger = logger;
    }

    /// <summary>
    /// Processes a payment charge via the external payment gateway using Flurl.
    /// </summary>
    [HttpPost("charges")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Charge([FromBody] ChargeRequest request)
    {
        if (request.Amount <= 0)
        {
            return BadRequest(new { message = "Amount must be greater than 0." });
        }

        try
        {
            var result = await _gatewayService.ChargeAsync(request);
            return Ok(result);
        }
        catch (FlurlHttpException ex)
        {
            _logger.LogError(ex, "Payment charge failed on external gateway with status {Status}", ex.StatusCode);
            var errorResponse = await ex.GetResponseJsonAsync<GatewayErrorResponse>();
            return StatusCode(ex.StatusCode ?? StatusCodes.Status502BadGateway, errorResponse ?? new GatewayErrorResponse("GATEWAY_ERROR", ex.Message));
        }
    }

    /// <summary>
    /// Retrieves a transaction status by ID.
    /// </summary>
    [HttpGet("transactions/{id}")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransaction(string id)
    {
        var transaction = await _gatewayService.GetTransactionAsync(id);
        if (transaction == null)
        {
            return NotFound(new { message = $"Transaction with ID '{id}' was not found." });
        }

        return Ok(transaction);
    }

    /// <summary>
    /// Processes a refund for an existing transaction.
    /// </summary>
    [HttpPost("refunds")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Refund([FromBody] RefundRequest request)
    {
        if (request.Amount <= 0)
        {
            return BadRequest(new { message = "Refund amount must be greater than 0." });
        }

        try
        {
            var result = await _gatewayService.RefundAsync(request);
            return Ok(result);
        }
        catch (FlurlHttpException ex)
        {
            _logger.LogError(ex, "Refund failed on external gateway");
            var errorResponse = await ex.GetResponseJsonAsync<GatewayErrorResponse>();
            return StatusCode(ex.StatusCode ?? StatusCodes.Status502BadGateway, errorResponse ?? new GatewayErrorResponse("REFUND_FAILED", ex.Message));
        }
    }

    /// <summary>
    /// Retrieves currency exchange rates with fluent query parameters.
    /// </summary>
    [HttpGet("rates")]
    [ProducesResponseType(typeof(ExchangeRatesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetRates([FromQuery] string baseCurrency = "USD", [FromQuery] string symbols = "EUR,GBP,JPY")
    {
        if (string.IsNullOrWhiteSpace(baseCurrency))
        {
            return BadRequest(new { message = "baseCurrency is required." });
        }

        var symbolList = symbols.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var result = await _gatewayService.GetRatesAsync(baseCurrency, symbolList);
        return Ok(result);
    }
}
