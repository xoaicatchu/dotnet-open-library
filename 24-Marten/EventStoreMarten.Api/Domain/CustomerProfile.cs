namespace EventStoreMarten.Api.Domain;

public class CustomerProfile
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Tier { get; set; } = "Standard";
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
}
