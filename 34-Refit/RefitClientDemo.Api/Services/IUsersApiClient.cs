using Refit;
using RefitClientDemo.Api.Models;

namespace RefitClientDemo.Api.Services;

[Headers("User-Agent: RefitClientDemo/1.0", "Accept: application/json")]
public interface IUsersApiClient
{
    [Get("/api/external/users")]
    Task<ApiResponse<List<UserProfile>>> GetUsersAsync([Query] UserFilterQuery query, [Header("Authorization")] string? authorization = null);

    [Get("/api/external/users/{id}")]
    Task<ApiResponse<UserProfile>> GetUserByIdAsync(int id);

    [Post("/api/external/users")]
    Task<ApiResponse<UserProfile>> CreateUserAsync([Body] CreateUserProfileRequest request);

    [Put("/api/external/users/{id}")]
    Task<ApiResponse<UserProfile>> UpdateUserAsync(int id, [Body] UpdateUserProfileRequest request);

    [Delete("/api/external/users/{id}")]
    Task<IApiResponse> DeleteUserAsync(int id);
}
