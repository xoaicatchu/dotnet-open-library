using EventStoreMarten.Api.Domain;
using EventStoreMarten.Api.Models;

namespace EventStoreMarten.Api.Services;

public interface IBankAccountStore
{
    Task<Guid> CreateAccountAsync(string ownerName, decimal initialBalance);
    Task<BankAccount?> GetAccountAsync(Guid id);
    Task<bool> DepositAsync(Guid id, decimal amount, string description);
    Task<bool> WithdrawAsync(Guid id, decimal amount, string description);
    Task<IReadOnlyList<EventStreamItemDto>> GetEventStreamAsync(Guid id);

    // Document DB features
    Task<Guid> SaveCustomerAsync(CustomerProfile profile);
    Task<CustomerProfile?> GetCustomerAsync(Guid id);
}
