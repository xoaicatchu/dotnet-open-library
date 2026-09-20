using UserProfilesMapster.Api.Entities;

namespace UserProfilesMapster.Api.Data;

public class UserStore
{
    private readonly List<User> _users = new();
    private int _nextId = 1;

    public UserStore()
    {
        Seed();
    }

    private void Seed()
    {
        _users.Add(new User
        {
            Id = _nextId++,
            Username = "johndoe",
            Email = "john.doe@example.com",
            FirstName = "John",
            LastName = "Doe",
            Bio = "Senior .NET Software Architect",
            Roles = new List<string> { "User", "Admin" },
            Preferences = new UserPreferences
            {
                Theme = "Dark",
                EmailNotifications = true,
                Language = "en"
            },
            CreatedAt = DateTime.UtcNow.AddMonths(-3)
        });
    }

    public List<User> GetAll() => _users.ToList();

    public User? GetById(int id) => _users.FirstOrDefault(u => u.Id == id);

    public User Add(User user)
    {
        user.Id = _nextId++;
        user.CreatedAt = DateTime.UtcNow;
        _users.Add(user);
        return user;
    }
}
