using Microsoft.EntityFrameworkCore;
using CachingAdvanced.Api.Entities;

namespace CachingAdvanced.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ProductEntity> Products => Set<ProductEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductEntity>().HasData(
            new ProductEntity { Id = 1, Name = "Laptop", Price = 999.99m, Stock = 50 },
            new ProductEntity { Id = 2, Name = "Smartphone", Price = 499.99m, Stock = 100 },
            new ProductEntity { Id = 3, Name = "Headphones", Price = 99.99m, Stock = 200 }
        );
    }
}
