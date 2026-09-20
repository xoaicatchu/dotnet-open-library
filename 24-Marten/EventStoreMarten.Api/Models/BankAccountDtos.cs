namespace EventStoreMarten.Api.Models;

public record CreateAccountRequest(
    string OwnerName,
    decimal InitialBalance
);

public record DepositRequest(
    decimal Amount,
    string Description
);

public record WithdrawRequest(
    decimal Amount,
    string Description
);

public record AccountResponseDto(
    Guid Id,
    string OwnerName,
    decimal Balance,
    int Version,
    DateTime CreatedAt,
    DateTime LastUpdatedAt
);

public record EventStreamItemDto(
    string EventType,
    object Data,
    DateTime Timestamp
);

public record CreateCustomerRequest(
    string FullName,
    string Email,
    string PhoneNumber,
    string Tier
);

public record CustomerResponseDto(
    Guid Id,
    string FullName,
    string Email,
    string PhoneNumber,
    string Tier,
    DateTime RegisteredAt
);
