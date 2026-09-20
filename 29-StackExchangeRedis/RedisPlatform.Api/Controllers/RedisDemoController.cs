using Microsoft.AspNetCore.Mvc;
using RedisPlatform.Api.Models;
using RedisPlatform.Api.Services;

namespace RedisPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RedisDemoController : ControllerBase
{
    private readonly IRedisService _redis;

    public RedisDemoController(IRedisService redis)
    {
        _redis = redis;
    }

    /// <summary>
    /// String: Atomically increments a numeric counter (useful for rate limiting, page views).
    /// </summary>
    [HttpPost("counter/{key}")]
    [ProducesResponseType(typeof(CounterResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CounterResponseDto>> IncrementCounter(string key, [FromQuery] long increment = 1)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return BadRequest(new { message = "Key is required." });
        }

        var val = await _redis.IncrementCounterAsync(key.Trim(), increment);
        return Ok(new CounterResponseDto(key.Trim(), val));
    }

    /// <summary>
    /// Hash: Sets a field-value pair in a Redis hash (useful for user sessions, shopping carts).
    /// </summary>
    [HttpPost("hash/{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetHash(string key, [FromBody] HashFieldRequest request)
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(request.Field))
        {
            return BadRequest(new { message = "Key and Field are required." });
        }

        await _redis.SetHashAsync(key.Trim(), request.Field.Trim(), request.Value);
        return Ok(new { message = $"Field '{request.Field}' set in hash '{key}'." });
    }

    /// <summary>
    /// Hash: Retrieves all fields and values of a Redis hash.
    /// </summary>
    [HttpGet("hash/{key}")]
    [ProducesResponseType(typeof(HashResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<HashResponseDto>> GetHash(string key)
    {
        var entries = await _redis.GetHashAsync(key.Trim());
        return Ok(new HashResponseDto(key.Trim(), entries));
    }

    /// <summary>
    /// Set: Adds a unique member to a Redis set (useful for tags, unique visitor tracking).
    /// </summary>
    [HttpPost("set/{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddToSet(string key, [FromBody] SetMemberRequest request)
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(request.Member))
        {
            return BadRequest(new { message = "Key and Member are required." });
        }

        var added = await _redis.AddToSetAsync(key.Trim(), request.Member.Trim());
        return Ok(new { added, member = request.Member.Trim() });
    }

    /// <summary>
    /// Set: Gets all members of a Redis set.
    /// </summary>
    [HttpGet("set/{key}")]
    [ProducesResponseType(typeof(SetResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SetResponseDto>> GetSet(string key)
    {
        var members = await _redis.GetSetMembersAsync(key.Trim());
        return Ok(new SetResponseDto(key.Trim(), members));
    }

    /// <summary>
    /// Sorted Set: Adds or updates a member's score in a leaderboard (ranking system).
    /// </summary>
    [HttpPost("leaderboard/{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddToLeaderboard(string key, [FromBody] LeaderboardScoreRequest request)
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(request.Member))
        {
            return BadRequest(new { message = "Key and Member are required." });
        }

        await _redis.AddToLeaderboardAsync(key.Trim(), request.Member.Trim(), request.Score);
        return Ok(new { message = $"Member '{request.Member}' scored {request.Score} in leaderboard '{key}'." });
    }

    /// <summary>
    /// Sorted Set: Gets top ranked members ordered by score descending.
    /// </summary>
    [HttpGet("leaderboard/{key}/top")]
    [ProducesResponseType(typeof(IEnumerable<LeaderboardEntryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<LeaderboardEntryDto>>> GetTopLeaderboard(string key, [FromQuery] int take = 10)
    {
        var entries = await _redis.GetTopLeaderboardAsync(key.Trim(), take);
        return Ok(entries);
    }

    /// <summary>
    /// Pub/Sub: Publishes a real-time message to a Redis channel.
    /// </summary>
    [HttpPost("publish")]
    [ProducesResponseType(typeof(PublishResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PublishResponseDto>> Publish([FromBody] PublishMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Channel) || string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { message = "Channel and Message are required." });
        }

        var receivers = await _redis.PublishAsync(request.Channel.Trim(), request.Message);
        return Ok(new PublishResponseDto(request.Channel.Trim(), receivers, request.Message));
    }
}
