using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
namespace StockFlow.Api.Auth;
[ApiController, Route("api/auth")]
public sealed class AuthController(SignInManager<IdentityUser> signIn, UserManager<IdentityUser> users, IAntiforgery antiforgery) : ControllerBase
{
    [HttpGet("csrf"), AllowAnonymous]
    public IActionResult Csrf()
    {
        Response.Headers.CacheControl = "no-store";
        return Ok(new { requestToken = antiforgery.GetAndStoreTokens(HttpContext).RequestToken });
    }
    [HttpPost("login"), AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await signIn.PasswordSignInAsync(request.Email.Trim(), request.Password, false, true);
        return result.Succeeded ? NoContent() : Unauthorized(new ProblemDetails { Status = 401, Title = "Email hoặc mật khẩu không hợp lệ." });
    }
    [HttpPost("logout"), Authorize]
    public async Task<IActionResult> Logout() { await signIn.SignOutAsync(); return NoContent(); }
    [HttpGet("me"), Authorize]
    public async Task<IActionResult> Me()
    {
        Response.Headers.CacheControl = "no-store";
        var user = await users.GetUserAsync(User);
        if (user is null) return Unauthorized();
        return Ok(new UserResponse(user.Id, user.Email!, (await users.GetRolesAsync(user)).ToArray()));
    }
}
