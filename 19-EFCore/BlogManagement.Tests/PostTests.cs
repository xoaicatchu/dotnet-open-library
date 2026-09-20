using System.Net;
using System.Net.Http.Json;
using BlogManagement.Api.Data;
using BlogManagement.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BlogManagement.Tests;

public class PostTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _dbName;

    public PostTests(WebApplicationFactory<Program> factory)
    {
        _dbName = Guid.NewGuid().ToString();
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<BlogDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<BlogDbContext>(options =>
                {
                    options.UseSqlite($"Data Source={_dbName}.db");
                });
            });
        });
    }

    [Fact]
    public async Task GetPosts_ReturnsSeededPosts()
    {
        var client = _factory.CreateClient();
        
        var response = await client.GetAsync("/api/posts");
        response.EnsureSuccessStatusCode();

        var posts = await response.Content.ReadFromJsonAsync<List<PostSummaryDto>>();
        Assert.NotNull(posts);
        Assert.Equal(2, posts.Count); // Seeded 2 posts
        Assert.Equal(2, posts[0].CommentsCount); // First post has 2 comments
    }

    [Fact]
    public async Task GetPost_ReturnsPostWithComments()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/posts/1");
        response.EnsureSuccessStatusCode();

        var post = await response.Content.ReadFromJsonAsync<PostDetailDto>();
        Assert.NotNull(post);
        Assert.Equal(1, post.Id);
        Assert.Equal(2, post.Comments.Count); // From Include
    }

    [Fact]
    public async Task CreatePost_ReturnsCreated()
    {
        var client = _factory.CreateClient();
        var newPost = new CreatePostDto("New Title", "New Content", "Author Name");

        var response = await client.PostAsJsonAsync("/api/posts", newPost);
        
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var getResponse = await client.GetAsync(response.Headers.Location);
        getResponse.EnsureSuccessStatusCode();
        var post = await getResponse.Content.ReadFromJsonAsync<PostDetailDto>();
        Assert.Equal("New Title", post!.Title);
    }

    [Fact]
    public async Task UpdatePost_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var update = new UpdatePostDto("Updated Title", "Updated Content");

        var response = await client.PutAsJsonAsync("/api/posts/2", update);
        response.EnsureSuccessStatusCode();

        var getResponse = await client.GetAsync("/api/posts/2");
        var post = await getResponse.Content.ReadFromJsonAsync<PostDetailDto>();
        
        Assert.Equal("Updated Title", post!.Title);
    }

    [Fact]
    public async Task AddComment_IncrementsCommentCount()
    {
        var client = _factory.CreateClient();
        
        // Initial comment count
        var getResponse1 = await client.GetAsync("/api/posts/2");
        var post1 = await getResponse1.Content.ReadFromJsonAsync<PostDetailDto>();
        var initialCount = post1!.Comments.Count;

        // Add comment
        var comment = new CreateCommentDto("New Commenter", "New Comment Content");
        var response = await client.PostAsJsonAsync("/api/posts/2/comments", comment);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Check new count
        var getResponse2 = await client.GetAsync("/api/posts/2");
        var post2 = await getResponse2.Content.ReadFromJsonAsync<PostDetailDto>();
        Assert.Equal(initialCount + 1, post2!.Comments.Count);
    }

    [Fact]
    public async Task DeletePost_CascadeDeletesComments()
    {
        var client = _factory.CreateClient();
        var newPost = new CreatePostDto("Temp", "Content", "Author");
        var postResponse = await client.PostAsJsonAsync("/api/posts", newPost);
        var createdUrl = postResponse.Headers.Location;

        var getResponse = await client.GetAsync(createdUrl);
        var post = await getResponse.Content.ReadFromJsonAsync<PostDetailDto>();
        var id = post!.Id;

        await client.PostAsJsonAsync($"/api/posts/{id}/comments", new CreateCommentDto("A", "C"));

        var deleteResponse = await client.DeleteAsync($"/api/posts/{id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var verifyResponse = await client.GetAsync($"/api/posts/{id}");
        Assert.Equal(HttpStatusCode.NotFound, verifyResponse.StatusCode);
    }

    [Fact]
    public async Task CreatePost_EmptyTitle_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var newPost = new CreatePostDto("", "Content", "Author");

        var response = await client.PostAsJsonAsync("/api/posts", newPost);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetPost_NonExistent_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/posts/999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SwaggerEndpoint_IsAccessible()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/swagger/v1/swagger.json"); // default url for swashbuckle
        
        response.EnsureSuccessStatusCode();
    }
}
