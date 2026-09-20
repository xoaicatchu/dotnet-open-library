using Bogus;
using GraphQL.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace GraphQL.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ProductEntity> Products { get; set; } = null!;
    public DbSet<OrderEntity> Orders { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<ProductEntity>()
            .HasMany(p => p.Orders)
            .WithOne(o => o.Product)
            .HasForeignKey(o => o.ProductId);
    }

    public static void Seed(AppDbContext context)
    {
        if (context.Products.Any()) return;

        var productFaker = new Faker<ProductEntity>()
            .RuleFor(p => p.Name, f => f.Commerce.ProductName())
            .RuleFor(p => p.Description, f => f.Commerce.ProductDescription())
            .RuleFor(p => p.Price, f => decimal.Parse(f.Commerce.Price(1, 1000)))
            .RuleFor(p => p.Stock, f => f.Random.Int(10, 100))
            .RuleFor(p => p.CategoryId, f => f.Random.Int(1, 10));

        var products = productFaker.Generate(20);
        context.Products.AddRange(products);
        context.SaveChanges();
    }
}
