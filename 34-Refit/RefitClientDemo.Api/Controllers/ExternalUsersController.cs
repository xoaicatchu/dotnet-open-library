using Microsoft.AspNetCore.Mvc;
using RefitClientDemo.Api.Models;
using RefitClientDemo.Api.Services;

namespace RefitClientDemo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExternalUsersController : ControllerBase
{
    private readonly IUsersApiClient _usersApi;
    private readonly ILogger<ExternalUsersController> _logger;

    public ExternalUsersController(IUsersApiClient usersApi, ILogger<ExternalUsersController> logger)
    {
        _usersApi = usersApi;
        _logger = logger;
    }

    /// <summary>
    /// Fetches users from external API using Refit client with query filters.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<UserProfile>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> GetUsers([FromQuery] UserFilterQuery query)
    {
        var response = await _usersApi.GetUsersAsync(query);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("External API failed with status {StatusCode}: {Error}", response.StatusCode, response.Error?.Message);
            return StatusCode((int)(response.StatusCode ?? System.Net.HttpStatusCode.BadGateway), new { message = "External API call failed", error = response.Error?.Message });
        }

        return Ok(response.Content);
    }

    /// <summary>
    /// Fetches user by ID from external API.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UserProfile), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById(int id)
    {
        var response = await _usersApi.GetUserByIdAsync(id);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound(new { message = $"User with ID {id} not found in external system." });
        }

        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)(response.StatusCode ?? System.Net.HttpStatusCode.BadGateway), new { message = "External API call failed" });
        }

        return Ok(response.Content);
    }

    /// <summary>
    /// Creates a user via external API through Refit.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(UserProfile), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { message = "Name and Email are required." });
        }

        var response = await _usersApi.CreateUserAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)(response.StatusCode ?? System.Net.HttpStatusCode.BadGateway), new { message = "Failed to create user via external API" });
        }

        var created = response.Content!;
        return CreatedAtAction(nameof(GetUserById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Updates an existing user via external API.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(UserProfile), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserProfileRequest request)
    {
        var response = await _usersApi.UpdateUserAsync(id, request);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound(new { message = $"User with ID {id} not found." });
        }

        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)(response.StatusCode ?? System.Net.HttpStatusCode.BadGateway), new { message = "Failed to update user" });
        }

        return Ok(response.Content);
    }

    /// <summary>
    /// Deletes a user via external API.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var response = await _usersApi.DeleteUserAsync(id);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound(new { message = $"User with ID {id} not found." });
        }

        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)(response.StatusCode ?? System.Net.HttpStatusCode.BadGateway), new { message = "Failed to delete user" });
        }

        return NoContent();
    }
}
