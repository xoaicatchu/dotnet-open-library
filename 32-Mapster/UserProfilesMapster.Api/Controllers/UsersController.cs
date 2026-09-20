using Mapster;
using Microsoft.AspNetCore.Mvc;
using UserProfilesMapster.Api.Data;
using UserProfilesMapster.Api.Entities;
using UserProfilesMapster.Api.Models;

namespace UserProfilesMapster.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserStore _store;

    public UsersController(UserStore store)
    {
        _store = store;
    }

    /// <summary>
    /// Gets all users projected to UserSummaryDto using Mapster .Adapt<T>().
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UserSummaryDto>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<UserSummaryDto>> GetAll()
    {
        var users = _store.GetAll();
        var dtos = users.Adapt<List<UserSummaryDto>>();
        return Ok(dtos);
    }

    /// <summary>
    /// Gets user details by ID mapped to UserDetailDto.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<UserDetailDto> GetById(int id)
    {
        var user = _store.GetById(id);
        if (user == null)
        {
            return NotFound(new { message = $"User with ID {id} not found." });
        }

        var dto = user.Adapt<UserDetailDto>();
        return Ok(dto);
    }

    /// <summary>
    /// Creates a new user by mapping CreateUserRequest to User entity via Mapster.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<UserDetailDto> Create([FromBody] CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { message = "Username and Email are required." });
        }

        var user = request.Adapt<User>();
        var created = _store.Add(user);
        var resultDto = created.Adapt<UserDetailDto>();

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, resultDto);
    }

    /// <summary>
    /// Updates user bio and theme by adapting request onto existing User entity.
    /// </summary>
    [HttpPut("{id:int}/bio")]
    [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<UserDetailDto> UpdateBio(int id, [FromBody] UpdateUserBioRequest request)
    {
        var user = _store.GetById(id);
        if (user == null)
        {
            return NotFound(new { message = $"User with ID {id} not found." });
        }

        user.Bio = request.Bio ?? user.Bio;
        if (!string.IsNullOrWhiteSpace(request.Theme))
        {
            user.Preferences.Theme = request.Theme;
        }

        var dto = user.Adapt<UserDetailDto>();
        return Ok(dto);
    }
}
