using Bogus;
using TestingMastery.Api.Services;

namespace TestingMastery.Tests.Snapshot;

public class SnapshotTests
{
    [Fact]
    public async Task ProductDto_Snapshot()
    {
        var product = new ProductDto(1, "iPhone 15", 999.99m, 50);
        await Verify(product);
    }
    
    [Fact]
    public async Task ProductList_Snapshot()
    {
        Randomizer.Seed = new Random(42);
        var products = new Faker<ProductDto>()
            .CustomInstantiator(f => new ProductDto(
                f.IndexFaker + 1,
                f.Commerce.ProductName(),
                Math.Round(f.Random.Decimal(10, 500), 2),
                f.Random.Int(1, 100)))
            .Generate(3);
        await Verify(products);
    }
}
