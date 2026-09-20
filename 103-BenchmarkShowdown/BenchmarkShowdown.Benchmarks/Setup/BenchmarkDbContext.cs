using BenchmarkShowdown.Benchmarks.Models;
using Microsoft.EntityFrameworkCore;

namespace BenchmarkShowdown.Benchmarks.Setup;

public class BenchmarkDbContext : DbContext
{
    public BenchmarkDbContext(DbContextOptions<BenchmarkDbContext> options) : base(options) { }

    public DbSet<ProductEntity> Products { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductEntity>().HasKey(x => x.Id);
    }
}
