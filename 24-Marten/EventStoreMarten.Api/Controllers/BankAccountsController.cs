using EventStoreMarten.Api.Domain;
using EventStoreMarten.Api.Models;
using EventStoreMarten.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventStoreMarten.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BankAccountsController : ControllerBase
{
    private readonly IBankAccountStore _store;

    public BankAccountsController(IBankAccountStore store)
    {
        _store = store;
    }

    /// <summary>
    /// Opens a new bank account by starting a new event stream with AccountCreated event.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(AccountResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AccountResponseDto>> Create([FromBody] CreateAccountRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.OwnerName))
        {
            return BadRequest(new { message = "Owner name is required." });
        }

        if (request.InitialBalance < 0)
        {
            return BadRequest(new { message = "Initial balance cannot be negative." });
        }

        var accountId = await _store.CreateAccountAsync(request.OwnerName.Trim(), request.InitialBalance);
        var account = await _store.GetAccountAsync(accountId);

        var response = new AccountResponseDto(
            account!.Id,
            account.OwnerName,
            account.Balance,
            account.Version,
            account.CreatedAt,
            account.LastUpdatedAt
        );

        return CreatedAtAction(nameof(GetById), new { id = accountId }, response);
    }

    /// <summary>
    /// Gets current account state by replaying all events in the stream (Live aggregation).
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AccountResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccountResponseDto>> GetById(Guid id)
    {
        var account = await _store.GetAccountAsync(id);
        if (account == null)
        {
            return NotFound(new { message = $"Bank account '{id}' not found." });
        }

        var response = new AccountResponseDto(
            account.Id,
            account.OwnerName,
            account.Balance,
            account.Version,
            account.CreatedAt,
            account.LastUpdatedAt
        );

        return Ok(response);
    }

    /// <summary>
    /// Appends a MoneyDeposited event to the account's event stream.
    /// </summary>
    [HttpPost("{id:guid}/deposit")]
    [ProducesResponseType(typeof(AccountResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccountResponseDto>> Deposit(Guid id, [FromBody] DepositRequest request)
    {
        if (request.Amount <= 0)
        {
            return BadRequest(new { message = "Deposit amount must be greater than 0." });
        }

        var success = await _store.DepositAsync(id, request.Amount, request.Description);
        if (!success)
        {
            return NotFound(new { message = $"Bank account '{id}' not found." });
        }

        var account = await _store.GetAccountAsync(id);
        return Ok(new AccountResponseDto(
            account!.Id,
            account.OwnerName,
            account.Balance,
            account.Version,
            account.CreatedAt,
            account.LastUpdatedAt
        ));
    }

    /// <summary>
    /// Appends a MoneyWithdrawn event to the account's event stream after checking balance.
    /// </summary>
    [HttpPost("{id:guid}/withdraw")]
    [ProducesResponseType(typeof(AccountResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccountResponseDto>> Withdraw(Guid id, [FromBody] WithdrawRequest request)
    {
        if (request.Amount <= 0)
        {
            return BadRequest(new { message = "Withdrawal amount must be greater than 0." });
        }

        var account = await _store.GetAccountAsync(id);
        if (account == null)
        {
            return NotFound(new { message = $"Bank account '{id}' not found." });
        }

        if (account.Balance < request.Amount)
        {
            return BadRequest(new { message = $"Insufficient funds. Current balance is {account.Balance:C}." });
        }

        var success = await _store.WithdrawAsync(id, request.Amount, request.Description);
        if (!success)
        {
            return BadRequest(new { message = "Failed to process withdrawal." });
        }

        var updated = await _store.GetAccountAsync(id);
        return Ok(new AccountResponseDto(
            updated!.Id,
            updated.OwnerName,
            updated.Balance,
            updated.Version,
            updated.CreatedAt,
            updated.LastUpdatedAt
        ));
    }

    /// <summary>
    /// Fetches the complete immutable event stream history for an account.
    /// </summary>
    [HttpGet("{id:guid}/events")]
    [ProducesResponseType(typeof(IEnumerable<EventStreamItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EventStreamItemDto>>> GetEvents(Guid id)
    {
        var events = await _store.GetEventStreamAsync(id);
        return Ok(events);
    }

    /// <summary>
    /// Document DB: Saves or updates a customer profile directly as a JSONB document.
    /// </summary>
    [HttpPost("customers")]
    [ProducesResponseType(typeof(CustomerResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CustomerResponseDto>> CreateCustomer([FromBody] CreateCustomerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { message = "FullName and Email are required." });
        }

        var customer = new CustomerProfile
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            Tier = string.IsNullOrWhiteSpace(request.Tier) ? "Standard" : request.Tier.Trim(),
            RegisteredAt = DateTime.UtcNow
        };

        var id = await _store.SaveCustomerAsync(customer);
        var created = await _store.GetCustomerAsync(id);

        var response = new CustomerResponseDto(
            created!.Id,
            created.FullName,
            created.Email,
            created.PhoneNumber,
            created.Tier,
            created.RegisteredAt
        );

        return CreatedAtAction(nameof(GetCustomerById), new { id }, response);
    }

    /// <summary>
    /// Document DB: Loads customer profile document by ID.
    /// </summary>
    [HttpGet("customers/{id:guid}")]
    [ProducesResponseType(typeof(CustomerResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerResponseDto>> GetCustomerById(Guid id)
    {
        var customer = await _store.GetCustomerAsync(id);
        if (customer == null)
        {
            return NotFound(new { message = $"Customer profile '{id}' not found." });
        }

        var response = new CustomerResponseDto(
            customer.Id,
            customer.FullName,
            customer.Email,
            customer.PhoneNumber,
            customer.Tier,
            customer.RegisteredAt
        );

        return Ok(response);
    }
}
