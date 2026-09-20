using EventStoreMarten.Api.Domain;
using EventStoreMarten.Api.Models;
using Marten;

namespace EventStoreMarten.Api.Services;

public class MartenBankAccountStore : IBankAccountStore
{
    private readonly IDocumentSession _session;

    public MartenBankAccountStore(IDocumentSession session)
    {
        _session = session;
    }

    public async Task<Guid> CreateAccountAsync(string ownerName, decimal initialBalance)
    {
        var accountId = Guid.NewGuid();
        var createdEvent = new AccountCreated(accountId, ownerName, initialBalance, DateTime.UtcNow);

        // Marten: Start a new event stream with aggregate type BankAccount
        _session.Events.StartStream<BankAccount>(accountId, createdEvent);
        await _session.SaveChangesAsync();

        return accountId;
    }

    public async Task<BankAccount?> GetAccountAsync(Guid id)
    {
        // Marten: Live stream aggregation replaying all events in stream
        return await _session.Events.AggregateStreamAsync<BankAccount>(id);
    }

    public async Task<bool> DepositAsync(Guid id, decimal amount, string description)
    {
        var account = await GetAccountAsync(id);
        if (account == null) return false;

        var depositEvent = new MoneyDeposited(id, amount, description, DateTime.UtcNow);
        _session.Events.Append(id, depositEvent);
        await _session.SaveChangesAsync();
        return true;
    }

    public async Task<bool> WithdrawAsync(Guid id, decimal amount, string description)
    {
        var account = await GetAccountAsync(id);
        if (account == null || account.Balance < amount) return false;

        var withdrawEvent = new MoneyWithdrawn(id, amount, description, DateTime.UtcNow);
        _session.Events.Append(id, withdrawEvent);
        await _session.SaveChangesAsync();
        return true;
    }

    public async Task<IReadOnlyList<EventStreamItemDto>> GetEventStreamAsync(Guid id)
    {
        var events = await _session.Events.FetchStreamAsync(id);
        return events.Select(e => new EventStreamItemDto(
            e.EventType.Name,
            e.Data,
            e.Timestamp.UtcDateTime
        )).ToList();
    }

    public async Task<Guid> SaveCustomerAsync(CustomerProfile profile)
    {
        if (profile.Id == Guid.Empty)
        {
            profile.Id = Guid.NewGuid();
        }

        // Marten: Store POCO document directly into PostgreSQL JSONB table
        _session.Store(profile);
        await _session.SaveChangesAsync();
        return profile.Id;
    }

    public async Task<CustomerProfile?> GetCustomerAsync(Guid id)
    {
        // Marten: Load POCO document by ID
        return await _session.LoadAsync<CustomerProfile>(id);
    }
}
