using BlogManagement.Api.Data;
using BlogManagement.Api.Entities;
using BlogManagement.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlogManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostsController : ControllerBase
{
    private readonly BlogDbContext _db;

    public PostsController(BlogDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PostSummaryDto>>> GetPosts([FromQuery] string? author)
    {
        var query = _db.Posts.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(author))
        {
            query = query.Where(p => p.Author.Contains(author));
        }

        var posts = await query
            .Select(p => new PostSummaryDto(p.Id, p.Title, p.Author, p.CreatedAt, p.Comments.Count))
            .ToListAsync();

        return Ok(posts);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PostDetailDto>> GetPost(int id)
    {
        var post = await _db.Posts
            .Include(p => p.Comments)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (post == null)
        {
            return NotFound();
        }

        var dto = new PostDetailDto(
            post.Id,
            post.Title,
            post.Content,
            post.Author,
            post.CreatedAt,
            post.Comments.Select(c => new CommentDto(c.Id, c.Author, c.Content, c.CreatedAt)).ToList()
        );

        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult> CreatePost([FromBody] CreatePostDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            return BadRequest("Title is required");
        }

        var post = new Post
        {
            Title = dto.Title,
            Content = dto.Content,
            Author = dto.Author,
            CreatedAt = DateTime.UtcNow
        };

        _db.Posts.Add(post);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPost), new { id = post.Id }, null);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdatePost(int id, [FromBody] UpdatePostDto dto)
    {
        var post = await _db.Posts.FindAsync(id);
        if (post == null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            return BadRequest("Title is required");
        }

        post.Title = dto.Title;
        post.Content = dto.Content;

        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeletePost(int id)
    {
        var post = await _db.Posts.FindAsync(id);
        if (post == null)
        {
            return NotFound();
        }

        _db.Posts.Remove(post);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id}/comments")]
    public async Task<ActionResult> AddComment(int id, [FromBody] CreateCommentDto dto)
    {
        var postExists = await _db.Posts.AnyAsync(p => p.Id == id);
        if (!postExists)
        {
            return NotFound();
        }

        var comment = new Comment
        {
            PostId = id,
            Author = dto.Author,
            Content = dto.Content,
            CreatedAt = DateTime.UtcNow
        };

        _db.Comments.Add(comment);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPost), new { id = id }, null); // Or returning comment itself, but user said 201 Created
    }
}
