using System;
using System.Collections.Generic;
namespace EventSourcing.Api.Domain.Common;

public abstract class AggregateRoot
{
    public Guid Id { get; protected set; }
    public int Version { get; private set; } = 0;
    
    private readonly List<IEvent> _uncommittedEvents = new();
    public IReadOnlyList<IEvent> UncommittedEvents => _uncommittedEvents.AsReadOnly();
    
    protected void RaiseEvent(IEvent @event)
    {
        Apply(@event);
        _uncommittedEvents.Add(@event);
        Version++;
    }
    
    public void LoadFromHistory(IEnumerable<IEvent> events)
    {
        foreach (var e in events)
        {
            Apply(e);
            Version++;
        }
    }
    
    protected abstract void Apply(IEvent @event);
    public void ClearUncommittedEvents() => _uncommittedEvents.Clear();
}
