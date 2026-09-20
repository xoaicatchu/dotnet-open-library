using System.Collections.Concurrent;
using EventStoreMarten.Api.Domain;
using EventStoreMarten.Api.Models;

namespace EventStoreMarten.Api.Services;

public class InMemoryBankAccountStore : IBankAccountStore
{
    private readonly ConcurrentDictionary<Guid, List<object>> _eventStreams = new();
    private readonly ConcurrentDictionary<Guid, CustomerProfile> _customers = new();

    public Task<Guid> CreateAccountAsync(string ownerName, decimal initialBalance)
    {
        var accountId = Guid.NewGuid();
        var created = new AccountCreated(accountId, ownerName, initialBalance, DateTime.UtcNow);
        _eventStreams[accountId] = new List<object> { created };
        return Task.FromResult(accountId);
    }

    public Task<BankAccount?> GetAccountAsync(Guid id)
    {
        if (!_eventStreams.TryGetValue(id, out var events))
        {
            return Task.FromResult<BankAccount?>(null);
        }

        var account = new BankAccount();
        lock (events)
        {
            foreach (var ev in events)
            {
                if (ev is AccountCreated ac) account.Apply(ac);
                else if (ev is MoneyDeposited md) account.Apply(md);
                else if (ev is MoneyWithdrawn mw) account.Apply(mw);
            }
            account.Version = events.Count;
        }
        return Task.FromResult<BankAccount?>(account);
    }

    public async Task<bool> DepositAsync(Guid id, decimal amount, string description)
    {
        if (!_eventStreams.TryGetValue(id, out var events))
        {
            return false;
        }

        var ev = new MoneyDeposited(id, amount, description, DateTime.UtcNow);
        lock (events)
        {
            events.Add(ev);
        }
        return true;
    }

    public async Task<bool> WithdrawAsync(Guid id, decimal amount, string description)
    {
        var account = await GetAccountAsync(id);
        if (account == null || account.Balance < amount)
        {
            return false;
        }

        if (!_eventStreams.TryGetValue(id, out var events))
        {
            return false;
        }

        var ev = new MoneyWithdrawn(id, amount, description, DateTime.UtcNow);
        lock (events)
        {
            events.Add(ev);
        }
        return true;
    }

    public Task<IReadOnlyList<EventStreamItemDto>> GetEventStreamAsync(Guid id)
    {
        if (!_eventStreams.TryGetValue(id, out var events))
        {
            return Task.FromResult<IReadOnlyList<EventStreamItemDto>>(Array.Empty<EventStreamItemDto>());
        }

        lock (events)
        {
            var dtos = events.Select(e =>
            {
                var ts = e switch
                {
                    AccountCreated ac => ac.CreatedAt,
                    MoneyDeposited md => md.DepositedAt,
                    MoneyWithdrawn mw => mw.WithdrawnAt,
                    _ => DateTime.UtcNow
                };
                return new EventStreamItemDto(e.GetType().Name, e, ts);
            }).ToList();
            return Task.FromResult<IReadOnlyList<EventStreamItemDto>>(dtos);
        }
    }

    public Task<Guid> SaveCustomerAsync(CustomerProfile profile)
    {
        if (profile.Id == Guid.Empty)
        {
            profile.Id = Guid.NewGuid();
        }
        _customers[profile.Id] = profile;
        return Task.FromResult(profile.Id);
    }

    public Task<CustomerProfile?> GetCustomerAsync(Guid id)
    {
        _customers.TryGetValue(id, out var customer);
        return Task.FromResult(customer);
    }
}
