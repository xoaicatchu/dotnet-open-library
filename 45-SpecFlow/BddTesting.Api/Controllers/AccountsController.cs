using BddTesting.Api.Models;
using BddTesting.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BddTesting.Api.Controllers;

[ApiController]
[Route("api/accounts")]
public class AccountsController : ControllerBase
{
    private readonly IBankService _bankService;

    public AccountsController(IBankService bankService)
    {
        _bankService = bankService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<BankAccount>), StatusCodes.Status200OK)]
    public IActionResult GetAll()
    {
        return Ok(_bankService.GetAllAccounts());
    }

    [HttpGet("{accountNumber}")]
    [ProducesResponseType(typeof(BankAccount), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetByAccountNumber([FromRoute] string accountNumber)
    {
        var account = _bankService.GetAccount(accountNumber);
        if (account == null)
        {
            return NotFound(new { error = $"Account '{accountNumber}' not found." });
        }

        return Ok(account);
    }

    [HttpPost]
    [ProducesResponseType(typeof(BankAccount), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult CreateAccount([FromBody] CreateAccountRequest request)
    {
        try
        {
            var account = _bankService.CreateAccount(request);
            return CreatedAtAction(nameof(GetByAccountNumber), new { accountNumber = account.AccountNumber }, account);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("transfer")]
    [ProducesResponseType(typeof(TransferResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Transfer([FromBody] TransferRequest request)
    {
        try
        {
            var result = _bankService.Transfer(request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
