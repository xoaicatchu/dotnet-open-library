using BookStore.Api.Features.Books;
using BookStore.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Wolverine;

namespace BookStore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    private readonly IMessageBus _bus;

    public BooksController(IMessageBus bus)
    {
        _bus = bus;
    }

    [HttpGet]
    public async Task<IResult> List([FromQuery] string? author)
    {
        var result = await _bus.InvokeAsync<IEnumerable<Book>>(new ListBooksQuery(author));
        return Results.Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IResult> Get(int id)
    {
        return await _bus.InvokeAsync<IResult>(new GetBookQuery(id));
    }

    [HttpPost]
    public async Task<IResult> Create([FromBody] CreateBookCommand cmd)
    {
        return await _bus.InvokeAsync<IResult>(cmd);
    }

    [HttpPut("{id:int}")]
    public async Task<IResult> Update(int id, [FromBody] UpdateBookRequest request)
    {
        var cmd = new UpdateBookCommand(id, request.Title, request.Author, request.Isbn, request.Price, request.PublishedYear);
        return await _bus.InvokeAsync<IResult>(cmd);
    }

    [HttpDelete("{id:int}")]
    public async Task<IResult> Delete(int id)
    {
        return await _bus.InvokeAsync<IResult>(new DeleteBookCommand(id));
    }
}

public record UpdateBookRequest(string Title, string Author, string? Isbn, decimal Price, int? PublishedYear);
