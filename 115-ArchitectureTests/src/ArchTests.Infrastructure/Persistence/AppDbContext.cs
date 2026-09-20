using ArchTests.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ArchTests.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    
    public DbSet<Product> Products { get; set; }
}
