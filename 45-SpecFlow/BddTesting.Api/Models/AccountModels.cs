namespace BddTesting.Api.Models;

public record BankAccount(
    string AccountNumber,
    string AccountHolder,
    decimal Balance,
    string Currency,
    bool IsActive,
    DateTime CreatedAt
);

public record CreateAccountRequest(
    string AccountNumber,
    string AccountHolder,
    decimal InitialDeposit = 0m,
    string Currency = "USD"
);

public record TransferRequest(
    string FromAccount,
    string ToAccount,
    decimal Amount,
    string Note = ""
);

public record TransferResult(
    Guid TransactionId,
    string FromAccount,
    string ToAccount,
    decimal Amount,
    decimal FromRemainingBalance,
    decimal ToNewBalance,
    DateTime Timestamp,
    string Status
);
