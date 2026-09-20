using System.Data;
using BenchmarkDotNet.Attributes;
using BenchmarkShowdown.Benchmarks.Models;
using BenchmarkShowdown.Benchmarks.Setup;
using Bogus;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace BenchmarkShowdown.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class OrmReadBenchmark
{
    private BenchmarkDbContext _dbContext = null!;
    private IDbConnection _connection = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Setup SQLite in-memory, seed 100 products
        var options = new DbContextOptionsBuilder<BenchmarkDbContext>()
            .UseSqlite("Data Source=benchmark-orm.db")
            .Options;
        _dbContext = new BenchmarkDbContext(options);
        _dbContext.Database.EnsureDeleted(); // Clear previous runs
        _dbContext.Database.EnsureCreated();
        
        // Seed nếu chưa có data
        if (!_dbContext.Products.Any())
        {
            var faker = new Faker<ProductEntity>()
                .RuleFor(p => p.Name, f => f.Commerce.ProductName())
                .RuleFor(p => p.Price, f => f.Random.Decimal(10, 500))
                .RuleFor(p => p.Stock, f => f.Random.Int(1, 100));
            _dbContext.Products.AddRange(faker.Generate(100));
            _dbContext.SaveChanges();
        }
        _connection = _dbContext.Database.GetDbConnection();
        if (_connection.State != ConnectionState.Open) _connection.Open();
    }

    [GlobalCleanup]
    public void Cleanup() { _dbContext.Dispose(); }

    [Benchmark(Baseline = true)]
    public async Task<List<ProductDto>> EFCore_AsNoTracking()
        => await _dbContext.Products.AsNoTracking()
            .Select(p => new ProductDto(p.Id, p.Name, p.Price, p.Stock))
            .ToListAsync();

    [Benchmark]
    public async Task<List<ProductDto>> Dapper_Query()
        => (await _connection.QueryAsync<ProductDto>("SELECT Id, Name, Price, Stock FROM Products")).AsList();
}
