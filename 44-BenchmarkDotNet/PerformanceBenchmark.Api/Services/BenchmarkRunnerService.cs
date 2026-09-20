using System.Diagnostics;
using PerformanceBenchmark.Api.Benchmarks;
using PerformanceBenchmark.Api.Models;

namespace PerformanceBenchmark.Api.Services;

public interface IBenchmarkRunnerService
{
    List<BenchmarkMetadata> GetAvailableBenchmarks();
    BenchmarkComparisonResult RunComparison(RunBenchmarkRequest request);
}

public class BenchmarkRunnerService : IBenchmarkRunnerService
{
    private static readonly List<BenchmarkMetadata> AvailableBenchmarks =
    [
        new(
            Id: "json",
            Name: "JSON Serialization",
            Category: "Serialization",
            Description: "Compares System.Text.Json against Newtonsoft.Json for object serialization.",
            Methods: ["SystemTextJson", "NewtonsoftJson"]
        ),
        new(
            Id: "string",
            Name: "String Concatenation",
            Category: "String Manipulation",
            Description: "Compares string + operator, StringBuilder, and string interpolation.",
            Methods: ["StringPlus", "StringBuilderConcatenation", "StringInterpolation"]
        ),
        new(
            Id: "collection",
            Name: "Collection Iteration & Sum",
            Category: "Collections",
            Description: "Compares indexed for loop, foreach loop, and LINQ Sum over 1,000 items.",
            Methods: ["ForLoop", "ForEachLoop", "LinqSum"]
        )
    ];

    public List<BenchmarkMetadata> GetAvailableBenchmarks() => AvailableBenchmarks;

    public BenchmarkComparisonResult RunComparison(RunBenchmarkRequest request)
    {
        var benchmark = AvailableBenchmarks.FirstOrDefault(b => b.Id.Equals(request.BenchmarkId, StringComparison.OrdinalIgnoreCase));
        if (benchmark == null)
        {
            throw new ArgumentException($"Benchmark with ID '{request.BenchmarkId}' does not exist.", nameof(request.BenchmarkId));
        }

        int iterations = Math.Clamp(request.Iterations, 100, 100_000);
        var swTotal = Stopwatch.StartNew();

        var results = new List<BenchmarkMethodResult>();

        switch (benchmark.Id.ToLowerInvariant())
        {
            case "json":
            {
                var bench = new JsonSerializationBenchmark();
                results.Add(Measure(bench.SystemTextJson, nameof(bench.SystemTextJson), iterations));
                results.Add(Measure(bench.NewtonsoftJson, nameof(bench.NewtonsoftJson), iterations));
                break;
            }
            case "string":
            {
                var bench = new StringConcatenationBenchmark();
                results.Add(Measure(bench.StringPlus, nameof(bench.StringPlus), iterations));
                results.Add(Measure(bench.StringBuilderConcatenation, nameof(bench.StringBuilderConcatenation), iterations));
                results.Add(Measure(bench.StringInterpolation, nameof(bench.StringInterpolation), iterations));
                break;
            }
            case "collection":
            {
                var bench = new CollectionIterationBenchmark();
                results.Add(Measure(bench.ForLoop, nameof(bench.ForLoop), iterations));
                results.Add(Measure(bench.ForEachLoop, nameof(bench.ForEachLoop), iterations));
                results.Add(Measure(bench.LinqSum, nameof(bench.LinqSum), iterations));
                break;
            }
        }

        swTotal.Stop();

        var winner = results.OrderBy(r => r.MeanMicroseconds).First().MethodName;

        return new BenchmarkComparisonResult(
            BenchmarkId: benchmark.Id,
            BenchmarkName: benchmark.Name,
            Iterations: iterations,
            Results: results,
            Winner: winner,
            ExecutionTimeMs: swTotal.ElapsedMilliseconds
        );
    }

    private static BenchmarkMethodResult Measure<T>(Func<T> action, string methodName, int iterations)
    {
        // Warmup (10% of iterations)
        int warmup = Math.Max(10, iterations / 10);
        for (int i = 0; i < warmup; i++)
        {
            _ = action();
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long bytesBefore = GC.GetAllocatedBytesForCurrentThread();
        var sw = Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
        {
            _ = action();
        }

        sw.Stop();
        long bytesAfter = GC.GetAllocatedBytesForCurrentThread();

        double totalMicroseconds = sw.Elapsed.TotalMicroseconds;
        double meanMicroseconds = Math.Round(totalMicroseconds / iterations, 4);
        long allocatedPerOp = Math.Max(0, (bytesAfter - bytesBefore) / iterations);
        double opsPerSec = meanMicroseconds > 0 ? Math.Round(1_000_000.0 / meanMicroseconds, 0) : 0;

        return new BenchmarkMethodResult(
            MethodName: methodName,
            MeanMicroseconds: meanMicroseconds,
            AllocatedBytesPerOp: allocatedPerOp,
            OperationsPerSecond: opsPerSec
        );
    }
}
