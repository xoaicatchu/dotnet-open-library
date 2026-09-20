using AuthStack.Api.Models;
using AuthStack.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthStack.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;

    public AuthController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        var user = await _userService.RegisterAsync(req);
        if (user == null) return Conflict("User already exists.");
        return Created("", new UserDto(user.Id, user.Username, user.Email, user.Role, user.IsActive));
    }

    [HttpPost("login")]
    public async Task<ActionResult<TokenResponse>> Login([FromBody] LoginRequest req)
    {
        var response = await _userService.LoginAsync(req);
        if (response == null) return Unauthorized("Invalid username or password.");
        return Ok(response);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<TokenResponse>> Refresh([FromBody] RefreshTokenRequest req)
    {
        var response = await _userService.RefreshAsync(req.RefreshToken);
        if (response == null) return Unauthorized("Invalid or expired refresh token.");
        return Ok(response);
    }

    [HttpPost("revoke")]
    [Authorize]
    public async Task<IActionResult> Revoke([FromBody] RevokeRequest req)
    {
        var result = await _userService.RevokeAsync(req.Username);
        if (!result) return NotFound("User not found.");
        return Ok();
    }
}
