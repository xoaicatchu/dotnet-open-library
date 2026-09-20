using BookStore.Api.Data;
using BookStore.Api.Models;
using Microsoft.AspNetCore.Http;

namespace BookStore.Api.Features.Books;

public record CreateBookCommand(string Title, string Author, string? Isbn, decimal Price, int? PublishedYear);

public static class CreateBookHandler
{
    public static IResult Handle(CreateBookCommand command, Data.BookStore store)
    {
        if (string.IsNullOrWhiteSpace(command.Title) || command.Title.Length > 300)
            return Results.BadRequest(new { Error = "Invalid title" });
            
        if (string.IsNullOrWhiteSpace(command.Author) || command.Author.Length > 200)
            return Results.BadRequest(new { Error = "Invalid author" });
            
        if (command.Price <= 0)
            return Results.BadRequest(new { Error = "Price must be > 0" });
            
        if (command.Isbn != null && command.Isbn.Length != 10 && command.Isbn.Length != 13)
            return Results.BadRequest(new { Error = "ISBN must be 10 or 13 characters" });
            
        if (command.PublishedYear.HasValue && (command.PublishedYear < 1450 || command.PublishedYear > 2030))
            return Results.BadRequest(new { Error = "Published year must be between 1450 and 2030" });

        var book = new Book
        {
            Title = command.Title,
            Author = command.Author,
            Isbn = command.Isbn,
            Price = command.Price,
            PublishedYear = command.PublishedYear
        };

        var created = store.Add(book);
        return Results.Created($"/api/books/{created.Id}", created);
    }
}
