using System.Collections.Concurrent;
using BddTesting.Api.Models;

namespace BddTesting.Api.Services;

public interface IBankService
{
    BankAccount? GetAccount(string accountNumber);
    BankAccount CreateAccount(CreateAccountRequest request);
    TransferResult Transfer(TransferRequest request);
    List<BankAccount> GetAllAccounts();
}

public class BankService : IBankService
{
    private readonly ConcurrentDictionary<string, BankAccount> _accounts = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    public BankService()
    {
        // Seed initial accounts
        _accounts["ACC-1001"] = new BankAccount("ACC-1001", "Alice Smith", 1000.00m, "USD", true, DateTime.UtcNow);
        _accounts["ACC-1002"] = new BankAccount("ACC-1002", "Bob Jones", 500.00m, "USD", true, DateTime.UtcNow);
        _accounts["ACC-1003"] = new BankAccount("ACC-1003", "Charlie Brown", 50.00m, "USD", false, DateTime.UtcNow); // inactive
    }

    public BankAccount? GetAccount(string accountNumber)
    {
        _accounts.TryGetValue(accountNumber, out var account);
        return account;
    }

    public BankAccount CreateAccount(CreateAccountRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.AccountNumber))
        {
            throw new ArgumentException("Account number is required.", nameof(request.AccountNumber));
        }

        if (string.IsNullOrWhiteSpace(request.AccountHolder))
        {
            throw new ArgumentException("Account holder name is required.", nameof(request.AccountHolder));
        }

        if (request.InitialDeposit < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.InitialDeposit), "Initial deposit cannot be negative.");
        }

        var account = new BankAccount(
            AccountNumber: request.AccountNumber,
            AccountHolder: request.AccountHolder,
            Balance: request.InitialDeposit,
            Currency: string.IsNullOrWhiteSpace(request.Currency) ? "USD" : request.Currency.ToUpperInvariant(),
            IsActive: true,
            CreatedAt: DateTime.UtcNow
        );

        if (!_accounts.TryAdd(account.AccountNumber, account))
        {
            throw new InvalidOperationException($"Account '{request.AccountNumber}' already exists.");
        }

        return account;
    }

    public TransferResult Transfer(TransferRequest request)
    {
        if (request.Amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.Amount), "Transfer amount must be greater than zero.");
        }

        if (string.Equals(request.FromAccount, request.ToAccount, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Source and destination accounts cannot be the same.");
        }

        lock (_lock)
        {
            if (!_accounts.TryGetValue(request.FromAccount, out var fromAcc))
            {
                throw new ArgumentException($"Source account '{request.FromAccount}' not found.");
            }

            if (!_accounts.TryGetValue(request.ToAccount, out var toAcc))
            {
                throw new ArgumentException($"Destination account '{request.ToAccount}' not found.");
            }

            if (!fromAcc.IsActive)
            {
                throw new InvalidOperationException($"Source account '{request.FromAccount}' is inactive.");
            }

            if (!toAcc.IsActive)
            {
                throw new InvalidOperationException($"Destination account '{request.ToAccount}' is inactive.");
            }

            if (fromAcc.Balance < request.Amount)
            {
                throw new InvalidOperationException(
                    $"Insufficient funds. Account '{fromAcc.AccountNumber}' has balance ${fromAcc.Balance:F2}, but transfer requires ${request.Amount:F2}.");
            }

            var updatedFrom = fromAcc with { Balance = fromAcc.Balance - request.Amount };
            var updatedTo = toAcc with { Balance = toAcc.Balance + request.Amount };

            _accounts[fromAcc.AccountNumber] = updatedFrom;
            _accounts[toAcc.AccountNumber] = updatedTo;

            return new TransferResult(
                TransactionId: Guid.NewGuid(),
                FromAccount: fromAcc.AccountNumber,
                ToAccount: toAcc.AccountNumber,
                Amount: request.Amount,
                FromRemainingBalance: updatedFrom.Balance,
                ToNewBalance: updatedTo.Balance,
                Timestamp: DateTime.UtcNow,
                Status: "Completed"
            );
        }
    }

    public List<BankAccount> GetAllAccounts()
    {
        return _accounts.Values.OrderBy(a => a.AccountNumber).ToList();
    }
}
