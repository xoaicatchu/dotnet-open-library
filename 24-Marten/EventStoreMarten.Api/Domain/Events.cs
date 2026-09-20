namespace EventStoreMarten.Api.Domain;

public record AccountCreated(
    Guid AccountId,
    string OwnerName,
    decimal InitialBalance,
    DateTime CreatedAt
);

public record MoneyDeposited(
    Guid AccountId,
    decimal Amount,
    string Description,
    DateTime DepositedAt
);

public record MoneyWithdrawn(
    Guid AccountId,
    decimal Amount,
    string Description,
    DateTime WithdrawnAt
);
