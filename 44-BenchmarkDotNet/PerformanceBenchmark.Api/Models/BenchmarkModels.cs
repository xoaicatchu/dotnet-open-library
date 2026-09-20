namespace PerformanceBenchmark.Api.Models;

public record BenchmarkMetadata(
    string Id,
    string Name,
    string Category,
    string Description,
    List<string> Methods
);

public record RunBenchmarkRequest(
    string BenchmarkId,
    int Iterations = 1000
);

public record BenchmarkMethodResult(
    string MethodName,
    double MeanMicroseconds,
    long AllocatedBytesPerOp,
    double OperationsPerSecond
);

public record BenchmarkComparisonResult(
    string BenchmarkId,
    string BenchmarkName,
    int Iterations,
    List<BenchmarkMethodResult> Results,
    string Winner,
    long ExecutionTimeMs
);
