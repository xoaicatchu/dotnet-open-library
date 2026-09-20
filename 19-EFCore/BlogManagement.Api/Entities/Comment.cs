namespace BlogManagement.Api.Entities;

public class Comment
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public string Author { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Post Post { get; set; } = null!;
}
