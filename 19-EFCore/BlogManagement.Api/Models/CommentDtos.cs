namespace BlogManagement.Api.Models;

public record CommentDto(int Id, string Author, string Content, DateTime CreatedAt);

public record CreateCommentDto(string Author, string Content);
