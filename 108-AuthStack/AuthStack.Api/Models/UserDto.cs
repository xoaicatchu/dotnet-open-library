namespace AuthStack.Api.Models;

public record UserDto(int Id, string Username, string Email, string Role, bool IsActive);
