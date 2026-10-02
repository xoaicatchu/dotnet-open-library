using System.ComponentModel.DataAnnotations;
namespace StockFlow.Api.Auth;
public sealed record LoginRequest([Required, EmailAddress] string Email, [Required] string Password);
public sealed record CreateUserRequest([Required, EmailAddress] string Email, [Required, MinLength(12)] string Password, [Required] string Role);
public sealed record UserResponse(string Id, string Email, IReadOnlyList<string> Roles);
