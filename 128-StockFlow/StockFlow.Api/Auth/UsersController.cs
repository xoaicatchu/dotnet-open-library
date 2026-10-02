using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace StockFlow.Api.Auth;
[ApiController, Route("api/users"), Authorize(Roles = RolePolicies.Admin)]
public sealed class UsersController(UserManager<IdentityUser> users) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (page < 1 || page > 1000000 || pageSize < 1 || pageSize > 100) return BadRequest();
        var items = await users.Users.OrderBy(x => x.Email).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        var result = new List<UserResponse>();
        foreach (var user in items) result.Add(new(user.Id, user.Email!, (await users.GetRolesAsync(user)).ToArray()));
        return Ok(result);
    }
    [HttpPost]
    public async Task<IActionResult> Create(CreateUserRequest request)
    {
        if (!RolePolicies.Roles.Contains(request.Role)) return BadRequest(new ProblemDetails { Title = "Vai trò không hợp lệ.", Status = 400 });
        var user = new IdentityUser { UserName = request.Email.Trim(), Email = request.Email.Trim() };
        var result = await users.CreateAsync(user, request.Password);
        if (!result.Succeeded) return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { ["user"] = result.Errors.Select(x => x.Description).ToArray() }));
        var roleResult = await users.AddToRoleAsync(user, request.Role);
        if (!roleResult.Succeeded) { await users.DeleteAsync(user); return Problem(statusCode: 500, title: "Không thể gán vai trò."); }
        return Created($"/api/users/{user.Id}", new UserResponse(user.Id, user.Email!, [request.Role]));
    }
}
