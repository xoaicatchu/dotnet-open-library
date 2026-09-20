using AutoMapper;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkShowdown.Benchmarks.Models;
using Bogus;
using Mapster;

namespace BenchmarkShowdown.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
public class MappingBenchmark
{
    private IMapper _autoMapper = null!;
    private ProductEntity _entity = null!;
    private List<ProductEntity> _entities = null!;

    [GlobalSetup]
    public void Setup()
    {
        var config = new MapperConfiguration(cfg => cfg.CreateMap<ProductEntity, ProductDto>());
        _autoMapper = config.CreateMapper();
        var faker = new Faker<ProductEntity>()
            .RuleFor(p => p.Id, f => f.IndexFaker + 1)
            .RuleFor(p => p.Name, f => f.Commerce.ProductName())
            .RuleFor(p => p.Price, f => f.Random.Decimal(10, 500))
            .RuleFor(p => p.Stock, f => f.Random.Int(1, 100));
        _entity = faker.Generate();
        _entities = faker.Generate(1000);
    }

    [Benchmark(Baseline = true)]
    public ProductDto AutoMapper_Single() => _autoMapper.Map<ProductDto>(_entity);

    [Benchmark]
    public ProductDto Mapster_Single() => _entity.Adapt<ProductDto>();

    [Benchmark]
    public List<ProductDto> AutoMapper_Bulk1000() => _autoMapper.Map<List<ProductDto>>(_entities);

    [Benchmark]
    public List<ProductDto> Mapster_Bulk1000() => _entities.Select(e => e.Adapt<ProductDto>()).ToList();
}
