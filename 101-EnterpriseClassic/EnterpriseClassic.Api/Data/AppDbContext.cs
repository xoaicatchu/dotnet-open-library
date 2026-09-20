using Bogus;
using EnterpriseClassic.Api.Features.Orders;
using EnterpriseClassic.Api.Features.Products;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseClassic.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<ProductEntity> Products => Set<ProductEntity>();
    public DbSet<OrderEntity> Orders => Set<OrderEntity>();
    public DbSet<OrderItemEntity> OrderItems => Set<OrderItemEntity>();
    
    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<ProductEntity>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.Price).HasColumnType("decimal(18,2)");
        });
        mb.Entity<OrderEntity>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
            e.HasMany(x => x.Items).WithOne().HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
        });
        mb.Entity<OrderItemEntity>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
        });
    }
    
    public static void Seed(AppDbContext ctx)
    {
        if (ctx.Products.Any()) return;
        Randomizer.Seed = new Random(101);
        var faker = new Faker<ProductEntity>()
            .RuleFor(p => p.Name, f => f.Commerce.ProductName())
            .RuleFor(p => p.Description, f => f.Lorem.Sentence())
            .RuleFor(p => p.Price, f => Math.Round(f.Random.Decimal(10, 500), 2))
            .RuleFor(p => p.Stock, f => f.Random.Int(1, 100))
            .RuleFor(p => p.CategoryId, f => f.Random.Int(1, 5))
            .RuleFor(p => p.CreatedAt, f => f.Date.Recent(30));
        ctx.Products.AddRange(faker.Generate(5));
        ctx.SaveChanges();
    }
}
