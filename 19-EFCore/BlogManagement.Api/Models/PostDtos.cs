namespace BlogManagement.Api.Models;

public record PostSummaryDto(int Id, string Title, string Author, DateTime CreatedAt, int CommentsCount);

public record PostDetailDto(int Id, string Title, string Content, string Author, DateTime CreatedAt, List<CommentDto> Comments);

public record CreatePostDto(string Title, string Content, string Author);

public record UpdatePostDto(string Title, string Content);
