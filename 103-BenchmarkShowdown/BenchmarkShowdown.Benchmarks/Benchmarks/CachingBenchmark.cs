using BenchmarkDotNet.Attributes;
using BenchmarkShowdown.Benchmarks.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using ZiggyCreatures.Caching.Fusion;

namespace BenchmarkShowdown.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class CachingBenchmark
{
    private IMemoryCache _memoryCache = null!;
    private IFusionCache _fusionCache = null!;
    private ProductDto _product = null!;

    [GlobalSetup]
    public void Setup()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        var services = new ServiceCollection();
        services.AddFusionCache();
        var sp = services.BuildServiceProvider();
        _fusionCache = sp.GetRequiredService<IFusionCache>();
        _product = new ProductDto(1, "Test Product", 99.99m, 10);
    }

    [Benchmark(Baseline = true)]
    public ProductDto IMemoryCache_GetOrCreate()
        => _memoryCache.GetOrCreate("product:1", entry => { entry.SlidingExpiration = TimeSpan.FromMinutes(5); return _product; })!;

    [Benchmark]
    public async ValueTask<ProductDto> FusionCache_GetOrSet()
        => await _fusionCache.GetOrSetAsync<ProductDto>("product:1",
            _ => Task.FromResult(_product),
            new FusionCacheEntryOptions { Duration = TimeSpan.FromMinutes(5) }); // Fusion cache 2.x doesn't use TimeSpan directly, Wait let's use the default or TimeSpan. Let's try TimeSpan.
}
