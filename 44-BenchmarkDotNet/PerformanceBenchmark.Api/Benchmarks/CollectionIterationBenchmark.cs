using BenchmarkDotNet.Attributes;

namespace PerformanceBenchmark.Api.Benchmarks;

[MemoryDiagnoser]
public class CollectionIterationBenchmark
{
    private static readonly List<int> Numbers = Enumerable.Range(1, 1000).ToList();

    [Benchmark(Baseline = true)]
    public long ForLoop()
    {
        long sum = 0;
        for (int i = 0; i < Numbers.Count; i++)
        {
            sum += Numbers[i];
        }
        return sum;
    }

    [Benchmark]
    public long ForEachLoop()
    {
        long sum = 0;
        foreach (var num in Numbers)
        {
            sum += num;
        }
        return sum;
    }

    [Benchmark]
    public long LinqSum()
    {
        return Numbers.Sum(x => (long)x);
    }
}
