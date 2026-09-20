namespace EventStoreMarten.Api.Domain;

public class BankAccount
{
    public Guid Id { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdatedAt { get; set; }

    // Marten Live Aggregation convention: Apply methods
    public void Apply(AccountCreated @event)
    {
        Id = @event.AccountId;
        OwnerName = @event.OwnerName;
        Balance = @event.InitialBalance;
        CreatedAt = @event.CreatedAt;
        LastUpdatedAt = @event.CreatedAt;
    }

    public void Apply(MoneyDeposited @event)
    {
        Balance += @event.Amount;
        LastUpdatedAt = @event.DepositedAt;
    }

    public void Apply(MoneyWithdrawn @event)
    {
        Balance -= @event.Amount;
        LastUpdatedAt = @event.WithdrawnAt;
    }
}
