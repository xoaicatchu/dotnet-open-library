using System.Collections.Concurrent;
using BookStore.Api.Models;

namespace BookStore.Api.Data;

public class BookStore
{
    private readonly ConcurrentDictionary<int, Book> _books = new();
    private int _nextId = 1;

    public BookStore()
    {
        var book1 = new Book { Id = GetNextId(), Title = "Clean Code", Author = "Robert C. Martin", Price = 45.00m, PublishedYear = 2008, Isbn = "0132350882" };
        var book2 = new Book { Id = GetNextId(), Title = "Design Patterns", Author = "Gang of Four", Price = 55.00m, PublishedYear = 1994, Isbn = "0201633612" };
        
        _books.TryAdd(book1.Id, book1);
        _books.TryAdd(book2.Id, book2);
    }

    private int GetNextId() => Interlocked.Increment(ref _nextId);

    public IEnumerable<Book> GetAll() => _books.Values;
    
    public Book? GetById(int id) => _books.TryGetValue(id, out var book) ? book : null;

    public Book Add(Book book)
    {
        book.Id = GetNextId();
        _books.TryAdd(book.Id, book);
        return book;
    }

    public bool Update(Book book)
    {
        if (!_books.ContainsKey(book.Id)) return false;
        _books[book.Id] = book;
        return true;
    }

    public bool Delete(int id)
    {
        return _books.TryRemove(id, out _);
    }
}
