namespace CleanVerticalSlice.Infrastructure.Persistence;

using System.Linq;
using Bogus;
using CleanVerticalSlice.Domain.Products;

public static class DbSeeder
{
    public static void Seed(AppDbContext context)
    {
        if (!context.Products.Any())
        {
            var productFaker = new Faker<Product>()
                .CustomInstantiator(f => Product.Create(
                    f.Commerce.ProductName(),
                    f.Commerce.ProductDescription(),
                    f.Random.Decimal(10, 1000),
                    f.Random.Int(1, 100),
                    f.Random.Int(1, 10)
                ));
            
            var products = productFaker.Generate(10);
            foreach(var p in products) { p.ClearDomainEvents(); } // Prevent event firing during seed
            context.Products.AddRange(products);
            context.SaveChanges();
        }
    }
}
