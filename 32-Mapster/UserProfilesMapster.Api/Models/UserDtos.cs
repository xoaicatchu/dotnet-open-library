namespace UserProfilesMapster.Api.Models;

public record PreferencesDto(
    string Theme,
    bool EmailNotifications,
    string Language
);

public record UserSummaryDto(
    int Id,
    string Username,
    string FullName,
    int RolesCount,
    string Theme,
    DateTime CreatedAt
);

public record UserDetailDto(
    int Id,
    string Username,
    string Email,
    string FullName,
    string Bio,
    List<string> Roles,
    PreferencesDto Preferences,
    DateTime CreatedAt
);

public record CreateUserRequest(
    string Username,
    string Email,
    string FirstName,
    string LastName,
    string Bio,
    List<string> Roles,
    string Theme = "Light",
    string Language = "en"
);

public record UpdateUserBioRequest(
    string Bio,
    string Theme
);
