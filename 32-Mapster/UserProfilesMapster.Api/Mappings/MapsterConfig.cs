using Mapster;
using UserProfilesMapster.Api.Entities;
using UserProfilesMapster.Api.Models;

namespace UserProfilesMapster.Api.Mappings;

public static class MapsterConfig
{
    public static void RegisterMappings(TypeAdapterConfig config)
    {
        // User -> UserSummaryDto
        config.NewConfig<User, UserSummaryDto>()
            .Map(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}".Trim())
            .Map(dest => dest.RolesCount, src => src.Roles.Count)
            .Map(dest => dest.Theme, src => src.Preferences.Theme);

        // User -> UserDetailDto
        config.NewConfig<User, UserDetailDto>()
            .Map(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}".Trim());

        // CreateUserRequest -> User
        config.NewConfig<CreateUserRequest, User>()
            .Map(dest => dest.Preferences, src => new UserPreferences
            {
                Theme = src.Theme,
                Language = src.Language,
                EmailNotifications = true
            });
    }
}
