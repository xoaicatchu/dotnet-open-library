namespace BookStore.Api.Models;

public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? Isbn { get; set; }
    public decimal Price { get; set; }
    public int? PublishedYear { get; set; }
}
