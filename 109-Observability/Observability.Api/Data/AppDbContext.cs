using Microsoft.EntityFrameworkCore;
using Observability.Api.Entities;

namespace Observability.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    
    public DbSet<ProductEntity> Products => Set<ProductEntity>();
}
