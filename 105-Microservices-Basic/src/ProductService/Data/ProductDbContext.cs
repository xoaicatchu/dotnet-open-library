using Microsoft.EntityFrameworkCore;
using ProductService.Entities;
using Bogus;

namespace ProductService.Data;

public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options) { }
    
    public DbSet<ProductEntity> Products => Set<ProductEntity>();
    
    public static void Seed(ProductDbContext context)
    {
        if (!context.Products.Any())
        {
            var id = 1;
            var faker = new Faker<ProductEntity>()
                .RuleFor(p => p.Id, f => id++)
                .RuleFor(p => p.Name, f => f.Commerce.ProductName())
                .RuleFor(p => p.Price, f => decimal.Parse(f.Commerce.Price(1, 100)))
                .RuleFor(p => p.Stock, f => f.Random.Int(10, 100));
                
            context.Products.AddRange(faker.Generate(5));
            context.Products.Add(new ProductEntity { Id = 99, Name = "Test Product", Price = 10, Stock = 5 });
            context.SaveChanges();
        }
    }
}
