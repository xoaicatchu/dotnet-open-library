using AuthStack.Api.Entities;
using AuthStack.Api.Models;

namespace AuthStack.Api.Services;

public interface IUserService
{
    Task<UserEntity?> RegisterAsync(RegisterRequest req);
    Task<TokenResponse?> LoginAsync(LoginRequest req);
    Task<TokenResponse?> RefreshAsync(string refreshToken);
    Task<bool> RevokeAsync(string username);
}
