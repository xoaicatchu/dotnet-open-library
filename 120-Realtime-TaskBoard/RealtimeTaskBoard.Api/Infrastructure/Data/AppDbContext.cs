using Microsoft.EntityFrameworkCore;
using RealtimeTaskBoard.Api.Features.Activities;
using RealtimeTaskBoard.Api.Features.Boards;
using RealtimeTaskBoard.Api.Features.Columns;
using RealtimeTaskBoard.Api.Features.Tasks;

namespace RealtimeTaskBoard.Api.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<BoardEntity> Boards => Set<BoardEntity>();
    public DbSet<ColumnEntity> Columns => Set<ColumnEntity>();
    public DbSet<TaskEntity> Tasks => Set<TaskEntity>();
    public DbSet<ActivityEntity> Activities => Set<ActivityEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
