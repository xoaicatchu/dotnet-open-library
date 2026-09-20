namespace RefitClientDemo.Api.Models;

public record UserProfile(
    int Id,
    string Name,
    string Email,
    string Role,
    bool IsActive,
    DateTime CreatedAt
);

public record CreateUserProfileRequest(
    string Name,
    string Email,
    string Role
);

public record UpdateUserProfileRequest(
    string Name,
    string Email,
    string Role,
    bool IsActive
);

public record UserFilterQuery(
    string? Role = null,
    int Page = 1,
    int PageSize = 10
);
