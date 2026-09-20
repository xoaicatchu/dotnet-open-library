using System.Text;
using BenchmarkDotNet.Attributes;

namespace PerformanceBenchmark.Api.Benchmarks;

[MemoryDiagnoser]
public class StringConcatenationBenchmark
{
    private const string Part1 = "User_";
    private const int Part2 = 12345;
    private const string Part3 = "_Status_";
    private const string Part4 = "ACTIVE";

    [Benchmark(Baseline = true)]
    public string StringPlus()
    {
        return Part1 + Part2 + Part3 + Part4;
    }

    [Benchmark]
    public string StringBuilderConcatenation()
    {
        var sb = new StringBuilder(32);
        sb.Append(Part1);
        sb.Append(Part2);
        sb.Append(Part3);
        sb.Append(Part4);
        return sb.ToString();
    }

    [Benchmark]
    public string StringInterpolation()
    {
        return $"{Part1}{Part2}{Part3}{Part4}";
    }
}
