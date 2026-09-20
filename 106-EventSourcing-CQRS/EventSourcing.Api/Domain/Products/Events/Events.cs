using System;
using EventSourcing.Api.Domain.Common;
namespace EventSourcing.Api.Domain.Products.Events;

public record ProductCreated(Guid AggregateId, string Name, decimal Price, int Stock, DateTime OccurredAt, int Version = 1) : IEvent;
public record ProductUpdated(Guid AggregateId, string Name, decimal Price, int Stock, DateTime OccurredAt, int Version = 0) : IEvent;
public record ProductDeactivated(Guid AggregateId, DateTime OccurredAt, int Version = 0) : IEvent;
