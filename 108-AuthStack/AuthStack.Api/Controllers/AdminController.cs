using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthStack.Api.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    [HttpGet("dashboard")]
    public IActionResult Dashboard()
    {
        return Ok(new { Message = "Admin only" });
    }

    [HttpGet("users")]
    public IActionResult ListUsers()
    {
        return Ok(new { Message = "User list for admins" });
    }
}
