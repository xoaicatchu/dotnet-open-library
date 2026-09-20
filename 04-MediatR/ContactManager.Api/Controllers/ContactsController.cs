using ContactManager.Api.Features.Contacts;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ContactManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ContactsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? search)
    {
        var result = await _mediator.Send(new ListContactsQuery(search));
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var result = await _mediator.Send(new GetContactQuery(id));
        return result is not null ? Ok(result) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateContactCommand cmd)
    {
        var result = await _mediator.Send(cmd);
        return Created($"/api/contacts/{result.Id}", result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateContactRequest request)
    {
        var cmd = new UpdateContactCommand(id, request.FirstName, request.LastName, request.Email, request.Phone, request.Company);
        var result = await _mediator.Send(cmd);
        return result ? Ok() : NotFound();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _mediator.Send(new DeleteContactCommand(id));
        return result ? NoContent() : NotFound();
    }
}

public record UpdateContactRequest(string FirstName, string LastName, string Email, string? Phone, string? Company);
