using BenchmarkDotNet.Attributes;
using Newtonsoft.Json;

namespace PerformanceBenchmark.Api.Benchmarks;

public record Person(int Id, string Name, string Email, DateTime CreatedAt, List<string> Roles);

[MemoryDiagnoser]
public class JsonSerializationBenchmark
{
    private static readonly Person SamplePerson = new(
        1,
        "John Doe",
        "john.doe@example.com",
        new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        ["Admin", "Developer", "Tester"]
    );

    [Benchmark(Baseline = true)]
    public string SystemTextJson()
    {
        return System.Text.Json.JsonSerializer.Serialize(SamplePerson);
    }

    [Benchmark]
    public string NewtonsoftJson()
    {
        return JsonConvert.SerializeObject(SamplePerson);
    }
}
