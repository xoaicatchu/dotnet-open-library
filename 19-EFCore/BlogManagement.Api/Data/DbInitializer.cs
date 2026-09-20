using BlogManagement.Api.Entities;

namespace BlogManagement.Api.Data;

public static class DbInitializer
{
    public static void Initialize(BlogDbContext context)
    {
        if (context.Posts.Any())
        {
            return;   // DB has been seeded
        }

        var posts = new Post[]
        {
            new Post
            {
                Title = "Introduction to EF Core",
                Content = "EF Core is a lightweight, extensible, open source and cross-platform version of the popular Entity Framework data access technology.",
                Author = "Admin",
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                Comments = new List<Comment>
                {
                    new Comment { Author = "Reader1", Content = "Great introduction!", CreatedAt = DateTime.UtcNow.AddDays(-1) },
                    new Comment { Author = "Reader2", Content = "Thanks for the post.", CreatedAt = DateTime.UtcNow.AddHours(-12) }
                }
            },
            new Post
            {
                Title = "Advanced EF Core Performance",
                Content = "Learn about AsNoTracking, Eager Loading, and other performance optimizations in EF Core.",
                Author = "Admin",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                Comments = new List<Comment>
                {
                    new Comment { Author = "DevUser", Content = "Very helpful tips.", CreatedAt = DateTime.UtcNow.AddHours(-2) }
                }
            }
        };

        context.Posts.AddRange(posts);
        context.SaveChanges();
    }
}
