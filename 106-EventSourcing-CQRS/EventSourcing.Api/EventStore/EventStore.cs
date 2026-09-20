using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EventSourcing.Api.Domain.Common;
using EventSourcing.Api.Domain.Products.Events;
namespace EventSourcing.Api.EventStore;

public interface IEventStore
{
    Task AppendEventsAsync(Guid aggregateId, string aggregateType, IEnumerable<IEvent> events, int expectedVersion);
    Task<List<IEvent>> GetEventsAsync(Guid aggregateId, int fromVersion = 0);
    Task<T?> ReconstructAggregateAsync<T>(Guid aggregateId) where T : AggregateRoot, new();
}

public class EventStoreRecord
{
    public Guid Id { get; set; }
    public Guid AggregateId { get; set; }
    public string AggregateType { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
    public int Version { get; set; }
    public DateTime OccurredAt { get; set; }
}

public class EventStoreDbContext : DbContext
{
    public EventStoreDbContext(DbContextOptions<EventStoreDbContext> options) : base(options) { }
    public DbSet<EventStoreRecord> Events { get; set; } = null!;
}

public class SqliteEventStore : IEventStore
{
    private readonly EventStoreDbContext _db;
    
    public SqliteEventStore(EventStoreDbContext db)
    {
        _db = db;
    }

    public async Task AppendEventsAsync(Guid aggregateId, string aggregateType, IEnumerable<IEvent> events, int expectedVersion)
    {
        var currentVersion = await _db.Events
            .Where(e => e.AggregateId == aggregateId)
            .MaxAsync(e => (int?)e.Version) ?? -1;
        
        if (currentVersion != expectedVersion - 1 && expectedVersion > 0)
            throw new InvalidOperationException($"Concurrency conflict. Expected version {expectedVersion - 1}, got {currentVersion}");
        
        var version = expectedVersion;
        foreach (var @event in events)
        {
            _db.Events.Add(new EventStoreRecord {
                Id = Guid.NewGuid(),
                AggregateId = aggregateId,
                AggregateType = aggregateType,
                EventType = @event.GetType().Name,
                Data = JsonSerializer.Serialize(@event, @event.GetType(), new JsonSerializerOptions { WriteIndented = false }),
                Version = version++,
                OccurredAt = @event.OccurredAt
            });
        }
        await _db.SaveChangesAsync();
    }
    
    public async Task<List<IEvent>> GetEventsAsync(Guid aggregateId, int fromVersion = 0)
    {
        var records = await _db.Events
            .Where(e => e.AggregateId == aggregateId && e.Version >= fromVersion)
            .OrderBy(e => e.Version)
            .ToListAsync();
        return records.Select(Deserialize).ToList();
    }
    
    public async Task<T?> ReconstructAggregateAsync<T>(Guid aggregateId) where T : AggregateRoot, new()
    {
        var events = await GetEventsAsync(aggregateId);
        if (!events.Any()) return null;
        var aggregate = new T();
        aggregate.LoadFromHistory(events);
        return aggregate;
    }
    
    private IEvent Deserialize(EventStoreRecord record) => record.EventType switch
    {
        nameof(ProductCreated) => JsonSerializer.Deserialize<ProductCreated>(record.Data)!,
        nameof(ProductUpdated) => JsonSerializer.Deserialize<ProductUpdated>(record.Data)!,
        nameof(ProductDeactivated) => JsonSerializer.Deserialize<ProductDeactivated>(record.Data)!,
        _ => throw new InvalidOperationException($"Unknown event type: {record.EventType}")
    };
}
