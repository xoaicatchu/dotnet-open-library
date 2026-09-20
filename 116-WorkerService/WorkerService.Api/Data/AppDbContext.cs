using Microsoft.EntityFrameworkCore;
using WorkerService.Api.Entities;

namespace WorkerService.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<JobEntity> Jobs => Set<JobEntity>();
}
