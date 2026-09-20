using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EventSourcing.Api.EventStore;
namespace EventSourcing.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventStore _eventStore;
    public EventsController(IEventStore eventStore) => _eventStore = eventStore;

    [HttpGet("{aggregateId}")]
    public async Task<IActionResult> GetEvents(Guid aggregateId)
    {
        var events = await _eventStore.GetEventsAsync(aggregateId);
        return Ok(events);
    }
}
