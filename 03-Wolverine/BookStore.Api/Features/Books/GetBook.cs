using BookStore.Api.Data;
using Microsoft.AspNetCore.Http;

namespace BookStore.Api.Features.Books;

public record GetBookQuery(int Id);

public static class GetBookHandler
{
    public static IResult Handle(GetBookQuery query, Data.BookStore store)
    {
        var book = store.GetById(query.Id);
        return book is not null ? Results.Ok(book) : Results.NotFound();
    }
}
