using Microsoft.EntityFrameworkCore;
using TestingMastery.Api.Entities;

namespace TestingMastery.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<ProductEntity> Products { get; set; }
}
