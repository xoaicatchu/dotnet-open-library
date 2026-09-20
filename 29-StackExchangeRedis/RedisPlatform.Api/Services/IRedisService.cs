using RedisPlatform.Api.Models;

namespace RedisPlatform.Api.Services;

public interface IRedisService
{
    Task<long> IncrementCounterAsync(string key, long value = 1);
    Task SetHashAsync(string key, string field, string value);
    Task<Dictionary<string, string>> GetHashAsync(string key);
    Task<bool> AddToSetAsync(string key, string member);
    Task<List<string>> GetSetMembersAsync(string key);
    Task AddToLeaderboardAsync(string leaderboardKey, string member, double score);
    Task<List<LeaderboardEntryDto>> GetTopLeaderboardAsync(string leaderboardKey, int take = 10);
    Task<long> PublishAsync(string channel, string message);
}
