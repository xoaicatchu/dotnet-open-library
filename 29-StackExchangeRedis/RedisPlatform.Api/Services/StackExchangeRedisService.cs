using RedisPlatform.Api.Models;
using StackExchange.Redis;

namespace RedisPlatform.Api.Services;

public class StackExchangeRedisService : IRedisService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;

    public StackExchangeRedisService(IConnectionMultiplexer redis)
    {
        _redis = redis;
        _db = redis.GetDatabase();
    }

    public async Task<long> IncrementCounterAsync(string key, long value = 1)
    {
        return await _db.StringIncrementAsync(key, value);
    }

    public async Task SetHashAsync(string key, string field, string value)
    {
        await _db.HashSetAsync(key, field, value);
    }

    public async Task<Dictionary<string, string>> GetHashAsync(string key)
    {
        var entries = await _db.HashGetAllAsync(key);
        return entries.ToDictionary(e => e.Name.ToString(), e => e.Value.ToString());
    }

    public async Task<bool> AddToSetAsync(string key, string member)
    {
        return await _db.SetAddAsync(key, member);
    }

    public async Task<List<string>> GetSetMembersAsync(string key)
    {
        var members = await _db.SetMembersAsync(key);
        return members.Select(m => m.ToString()).ToList();
    }

    public async Task AddToLeaderboardAsync(string leaderboardKey, string member, double score)
    {
        await _db.SortedSetAddAsync(leaderboardKey, member, score);
    }

    public async Task<List<LeaderboardEntryDto>> GetTopLeaderboardAsync(string leaderboardKey, int take = 10)
    {
        var entries = await _db.SortedSetRangeByRankWithScoresAsync(
            leaderboardKey,
            start: 0,
            stop: take - 1,
            order: Order.Descending
        );

        var result = new List<LeaderboardEntryDto>();
        long rank = 1;
        foreach (var entry in entries)
        {
            result.Add(new LeaderboardEntryDto(entry.Element.ToString(), entry.Score, rank++));
        }

        return result;
    }

    public async Task<long> PublishAsync(string channel, string message)
    {
        var sub = _redis.GetSubscriber();
        return await sub.PublishAsync(RedisChannel.Literal(channel), message);
    }
}
