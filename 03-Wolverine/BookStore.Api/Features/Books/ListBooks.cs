using BookStore.Api.Data;
using BookStore.Api.Models;

namespace BookStore.Api.Features.Books;

public record ListBooksQuery(string? Author);

public static class ListBooksHandler
{
    public static IEnumerable<Book> Handle(ListBooksQuery query, Data.BookStore store)
    {
        var books = store.GetAll();
        if (!string.IsNullOrWhiteSpace(query.Author))
        {
            books = books.Where(b => b.Author.Contains(query.Author, StringComparison.OrdinalIgnoreCase));
        }
        return books;
    }
}
