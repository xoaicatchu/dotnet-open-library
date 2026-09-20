using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BookStore.Tests;

public record BookDto(int Id, string Title, string Author, string? Isbn, decimal Price, int? PublishedYear);

public class BookEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public BookEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CrudRoundTrip_ReturnsExpectedStatusCodes()
    {
        var client = _factory.CreateClient();

        // 1. Create
        var createResponse = await client.PostAsJsonAsync("/api/books", new
        {
            Title = "Test Book",
            Author = "Test Author",
            Price = 19.99m
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var location = createResponse.Headers.Location;
        Assert.NotNull(location);

        var createdBook = await createResponse.Content.ReadFromJsonAsync<BookDto>();
        Assert.NotNull(createdBook);
        Assert.True(createdBook.Id > 0);

        // 2. Get
        var getResponse = await client.GetAsync(location);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var book = await getResponse.Content.ReadFromJsonAsync<BookDto>();
        Assert.Equal("Test Book", book?.Title);

        // 3. Update
        var updateResponse = await client.PutAsJsonAsync($"/api/books/{createdBook.Id}", new
        {
            Title = "Updated Book",
            Author = "Test Author",
            Price = 29.99m
        });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        
        var updatedBook = await updateResponse.Content.ReadFromJsonAsync<BookDto>();
        Assert.Equal("Updated Book", updatedBook?.Title);

        // 4. Delete
        var deleteResponse = await client.DeleteAsync($"/api/books/{createdBook.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // 5. Get after delete
        var getAfterDelete = await client.GetAsync($"/api/books/{createdBook.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getAfterDelete.StatusCode);
    }

    [Fact]
    public async Task ListAndFilterByAuthor_ReturnsCorrectBooks()
    {
        var client = _factory.CreateClient();

        var getResponse = await client.GetAsync("/api/books?author=robert");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        
        var books = await getResponse.Content.ReadFromJsonAsync<List<BookDto>>();
        Assert.NotNull(books);
        Assert.Contains(books, b => b.Author.Contains("Robert C. Martin"));
    }

    [Fact]
    public async Task ValidationErrors_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/books", new
        {
            Title = "", // empty
            Author = "Author",
            Price = -5m // negative
        });
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task NonExistentId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/books/9999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Swagger_IsAvailable()
    {
        var client = _factory.WithWebHostBuilder(builder => 
        {
            builder.UseEnvironment("Development");
        }).CreateClient();
        
        var response = await client.GetAsync("/swagger/index.html");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
