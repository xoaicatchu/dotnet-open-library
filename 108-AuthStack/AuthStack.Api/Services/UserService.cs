using AuthStack.Api.Data;
using AuthStack.Api.Entities;
using AuthStack.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthStack.Api.Services;

public class UserService : IUserService
{
    private readonly AuthDbContext _db;
    private readonly ITokenService _tokenService;
    
    public UserService(AuthDbContext db, ITokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }
    
    public async Task<UserEntity?> RegisterAsync(RegisterRequest req)
    {
        if (await _db.Users.AnyAsync(u => u.Username == req.Username)) return null;
        var user = new UserEntity {
            Username = req.Username,
            Email = req.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Role = req.Role
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }
    
    public async Task<TokenResponse?> LoginAsync(LoginRequest req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == req.Username && u.IsActive);
        if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash)) return null;
        
        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _db.SaveChangesAsync();
        return new TokenResponse(accessToken, refreshToken, DateTime.UtcNow.AddMinutes(15));
    }
    
    public async Task<TokenResponse?> RefreshAsync(string refreshToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.RefreshToken == refreshToken && u.RefreshTokenExpiry > DateTime.UtcNow && u.IsActive);
        if (user == null) return null;
        // Rotate refresh token
        var newAccessToken = _tokenService.GenerateAccessToken(user);
        var newRefreshToken = _tokenService.GenerateRefreshToken();
        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _db.SaveChangesAsync();
        return new TokenResponse(newAccessToken, newRefreshToken, DateTime.UtcNow.AddMinutes(15));
    }
    
    public async Task<bool> RevokeAsync(string username)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null) return false;
        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;
        await _db.SaveChangesAsync();
        return true;
    }
}
