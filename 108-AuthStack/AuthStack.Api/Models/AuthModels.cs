namespace AuthStack.Api.Models;

public record LoginRequest(string Username, string Password);
public record RegisterRequest(string Username, string Email, string Password, string Role = "User");
public record TokenResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);
public record RefreshTokenRequest(string RefreshToken);
public record RevokeRequest(string Username);
