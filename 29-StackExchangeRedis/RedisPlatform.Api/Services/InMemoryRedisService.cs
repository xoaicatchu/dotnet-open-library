using System.Collections.Concurrent;
using RedisPlatform.Api.Models;

namespace RedisPlatform.Api.Services;

public class InMemoryRedisService : IRedisService
{
    private readonly ConcurrentDictionary<string, long> _counters = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _hashes = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _sets = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, double>> _sortedSets = new();

    public Task<long> IncrementCounterAsync(string key, long value = 1)
    {
        var result = _counters.AddOrUpdate(key, value, (_, oldVal) => oldVal + value);
        return Task.FromResult(result);
    }

    public Task SetHashAsync(string key, string field, string value)
    {
        var hash = _hashes.GetOrAdd(key, _ => new ConcurrentDictionary<string, string>());
        hash[field] = value;
        return Task.CompletedTask;
    }

    public Task<Dictionary<string, string>> GetHashAsync(string key)
    {
        if (_hashes.TryGetValue(key, out var hash))
        {
            return Task.FromResult(new Dictionary<string, string>(hash));
        }
        return Task.FromResult(new Dictionary<string, string>());
    }

    public Task<bool> AddToSetAsync(string key, string member)
    {
        var set = _sets.GetOrAdd(key, _ => new ConcurrentDictionary<string, byte>());
        var added = set.TryAdd(member, 0);
        return Task.FromResult(added);
    }

    public Task<List<string>> GetSetMembersAsync(string key)
    {
        if (_sets.TryGetValue(key, out var set))
        {
            return Task.FromResult(set.Keys.ToList());
        }
        return Task.FromResult(new List<string>());
    }

    public Task AddToLeaderboardAsync(string leaderboardKey, string member, double score)
    {
        var zset = _sortedSets.GetOrAdd(leaderboardKey, _ => new ConcurrentDictionary<string, double>());
        zset[member] = score;
        return Task.CompletedTask;
    }

    public Task<List<LeaderboardEntryDto>> GetTopLeaderboardAsync(string leaderboardKey, int take = 10)
    {
        if (!_sortedSets.TryGetValue(leaderboardKey, out var zset))
        {
            return Task.FromResult(new List<LeaderboardEntryDto>());
        }

        var sorted = zset
            .OrderByDescending(kv => kv.Value)
            .Take(take)
            .Select((kv, index) => new LeaderboardEntryDto(kv.Key, kv.Value, index + 1))
            .ToList();

        return Task.FromResult(sorted);
    }

    public Task<long> PublishAsync(string channel, string message)
    {
        // In-memory simulation: 1 receiver active
        return Task.FromResult(1L);
    }
}
