using PerformanceBenchmark.Api.Benchmarks;
using Xunit;

namespace PerformanceBenchmark.Tests;

public class BenchmarkExecutionTests
{
    [Fact]
    public void JsonSerializationBenchmark_MethodsReturnValidJson()
    {
        // Arrange
        var bench = new JsonSerializationBenchmark();

        // Act
        var stjResult = bench.SystemTextJson();
        var newtonsoftResult = bench.NewtonsoftJson();

        // Assert
        Assert.NotNull(stjResult);
        Assert.NotNull(newtonsoftResult);
        Assert.Contains("John Doe", stjResult);
        Assert.Contains("John Doe", newtonsoftResult);
    }

    [Fact]
    public void StringConcatenationBenchmark_MethodsReturnEqualString()
    {
        // Arrange
        var bench = new StringConcatenationBenchmark();

        // Act
        var plusResult = bench.StringPlus();
        var sbResult = bench.StringBuilderConcatenation();
        var interpResult = bench.StringInterpolation();

        // Assert
        Assert.Equal("User_12345_Status_ACTIVE", plusResult);
        Assert.Equal(plusResult, sbResult);
        Assert.Equal(plusResult, interpResult);
    }

    [Fact]
    public void CollectionIterationBenchmark_MethodsReturnEqualSum()
    {
        // Arrange
        var bench = new CollectionIterationBenchmark();

        // Act
        var forSum = bench.ForLoop();
        var forEachSum = bench.ForEachLoop();
        var linqSum = bench.LinqSum();

        // Assert
        Assert.Equal(500500, forSum);
        Assert.Equal(forSum, forEachSum);
        Assert.Equal(forSum, linqSum);
    }
}
