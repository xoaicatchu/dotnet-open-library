namespace UserProfilesMapster.Api.Entities;

public class UserPreferences
{
    public string Theme { get; set; } = "Light";
    public bool EmailNotifications { get; set; } = true;
    public string Language { get; set; } = "en";
}

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public UserPreferences Preferences { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
