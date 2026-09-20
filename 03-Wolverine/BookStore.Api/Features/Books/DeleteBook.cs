using BookStore.Api.Data;
using Microsoft.AspNetCore.Http;

namespace BookStore.Api.Features.Books;

public record DeleteBookCommand(int Id);

public static class DeleteBookHandler
{
    public static IResult Handle(DeleteBookCommand command, Data.BookStore store)
    {
        return store.Delete(command.Id) ? Results.NoContent() : Results.NotFound();
    }
}
