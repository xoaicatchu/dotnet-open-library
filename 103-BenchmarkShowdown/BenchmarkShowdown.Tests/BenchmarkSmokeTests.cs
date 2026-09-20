using BenchmarkShowdown.Benchmarks.Benchmarks;
using FluentAssertions;
using Xunit;

namespace BenchmarkShowdown.Tests;

public class BenchmarkSmokeTests
{
    [Fact]
    public void MappingBenchmark_Setup_And_Run_DoesNotThrow()
    {
        var bm = new MappingBenchmark();
        bm.Setup();
        var r1 = bm.AutoMapper_Single();
        var r2 = bm.Mapster_Single();
        r1.Should().NotBeNull();
        r2.Should().NotBeNull();
        r1.Name.Should().Be(r2.Name); 
    }
    
    [Fact]
    public void MappingBenchmark_Bulk_Results_Equal()
    {
        var bm = new MappingBenchmark();
        bm.Setup();
        var autoMapperResult = bm.AutoMapper_Bulk1000();
        var mapsterResult = bm.Mapster_Bulk1000();
        autoMapperResult.Should().HaveCount(1000);
        mapsterResult.Should().HaveCount(1000);
        autoMapperResult.Select(x => x.Name).Should().BeEquivalentTo(mapsterResult.Select(x => x.Name));
    }
    
    [Fact]
    public void ValidationBenchmark_Valid_Pass_Invalid_Fail()
    {
        var bm = new ValidationBenchmark();
        bm.Setup();
        bm.ManualValidation().Should().BeTrue();
        bm.FluentValidation_Valid().IsValid.Should().BeTrue();
        bm.FluentValidation_Invalid().IsValid.Should().BeFalse();
    }
    
    [Fact]
    public async Task OrmReadBenchmark_EFCore_Returns_Data()
    {
        var bm = new OrmReadBenchmark();
        bm.Setup();
        var result = await bm.EFCore_AsNoTracking();
        result.Should().NotBeEmpty();
        bm.Cleanup();
    }
    
    [Fact]
    public async Task OrmReadBenchmark_Dapper_Returns_Data()
    {
        var bm = new OrmReadBenchmark();
        bm.Setup();
        var result = await bm.Dapper_Query();
        result.Should().NotBeEmpty();
        bm.Cleanup();
    }
    
    [Fact]
    public async Task CachingBenchmark_BothCaches_Return_Same_Data()
    {
        var bm = new CachingBenchmark();
        bm.Setup();
        var r1 = bm.IMemoryCache_GetOrCreate();
        var r2 = await bm.FusionCache_GetOrSet();
        r1.Name.Should().Be(r2.Name);
        r1.Price.Should().Be(r2.Price);
    }
}
