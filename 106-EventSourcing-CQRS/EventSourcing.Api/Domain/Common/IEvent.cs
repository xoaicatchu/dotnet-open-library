using System;
namespace EventSourcing.Api.Domain.Common;

public interface IEvent
{
    Guid AggregateId { get; }
    int Version { get; }
    DateTime OccurredAt { get; }
}
