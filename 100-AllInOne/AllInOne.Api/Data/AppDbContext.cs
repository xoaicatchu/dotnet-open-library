using Bogus;
using Microsoft.EntityFrameworkCore;
using AllInOne.Api.Models;

namespace AllInOne.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<OrderEntity> Orders => Set<OrderEntity>();
    public DbSet<OrderItemEntity> OrderItems => Set<OrderItemEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<OrderEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CustomerName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CustomerEmail).IsRequired().HasMaxLength(150);
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Status).HasConversion<string>();
            entity.HasMany(e => e.Items)
                  .WithOne(i => i.Order)
                  .HasForeignKey(i => i.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItemEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProductName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
        });
    }

    public static void Seed(AppDbContext context)
    {
        if (context.Orders.Any()) return;

        // Using Bogus (Library 41) for realistic deterministic data generation
        Randomizer.Seed = new Random(2026);

        var itemFaker = new Faker<OrderItemEntity>()
            .RuleFor(i => i.ProductName, f => f.Commerce.ProductName())
            .RuleFor(i => i.Quantity, f => f.Random.Int(1, 5))
            .RuleFor(i => i.UnitPrice, f => Math.Round(f.Random.Decimal(10, 500), 2));

        var orderFaker = new Faker<OrderEntity>()
            .RuleFor(o => o.OrderNumber, f => $"ORD-{f.Random.AlphaNumeric(8).ToUpperInvariant()}")
            .RuleFor(o => o.CustomerName, f => f.Name.FullName())
            .RuleFor(o => o.CustomerEmail, f => f.Internet.Email())
            .RuleFor(o => o.Status, _ => OrderStatus.Submitted)
            .RuleFor(o => o.CreatedAt, f => f.Date.Recent(30))
            .RuleFor(o => o.Items, _ => itemFaker.Generate(2));

        var orders = orderFaker.Generate(3);
        foreach (var o in orders)
        {
            o.TotalAmount = o.Items.Sum(i => i.TotalPrice);
        }

        context.Orders.AddRange(orders);
        context.SaveChanges();
    }
}
