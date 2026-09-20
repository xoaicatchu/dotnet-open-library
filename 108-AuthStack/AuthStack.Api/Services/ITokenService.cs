using AuthStack.Api.Entities;

namespace AuthStack.Api.Services;

public interface ITokenService
{
    string GenerateAccessToken(UserEntity user);
    string GenerateRefreshToken();
}
