using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using PerformanceBenchmark.Api.Models;
using Xunit;

namespace PerformanceBenchmark.Tests;

public class BenchmarkControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public BenchmarkControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAvailableBenchmarks_Returns200WithMetadata()
    {
        // Act
        var response = await _client.GetAsync("/api/benchmarks");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var benchmarks = await response.Content.ReadFromJsonAsync<List<BenchmarkMetadata>>();
        Assert.NotNull(benchmarks);
        Assert.True(benchmarks.Count >= 3);
        Assert.Contains(benchmarks, b => b.Id == "json");
        Assert.Contains(benchmarks, b => b.Id == "string");
        Assert.Contains(benchmarks, b => b.Id == "collection");
    }

    [Fact]
    public async Task RunComparison_Json_Returns200WithMetrics()
    {
        // Arrange
        var request = new RunBenchmarkRequest("json", Iterations: 500);

        // Act
        var response = await _client.PostAsJsonAsync("/api/benchmarks/run", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<BenchmarkComparisonResult>();
        Assert.NotNull(result);
        Assert.Equal("json", result.BenchmarkId);
        Assert.Equal(2, result.Results.Count);
        Assert.False(string.IsNullOrWhiteSpace(result.Winner));
        Assert.All(result.Results, r =>
        {
            Assert.False(string.IsNullOrWhiteSpace(r.MethodName));
            Assert.True(r.MeanMicroseconds >= 0);
            Assert.True(r.OperationsPerSecond >= 0);
        });
    }

    [Fact]
    public async Task RunComparison_String_Returns200WithMetrics()
    {
        // Arrange
        var request = new RunBenchmarkRequest("string", Iterations: 500);

        // Act
        var response = await _client.PostAsJsonAsync("/api/benchmarks/run", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<BenchmarkComparisonResult>();
        Assert.NotNull(result);
        Assert.Equal("string", result.BenchmarkId);
        Assert.Equal(3, result.Results.Count);
    }

    [Fact]
    public async Task RunComparison_Collection_Returns200WithMetrics()
    {
        // Arrange
        var request = new RunBenchmarkRequest("collection", Iterations: 200);

        // Act
        var response = await _client.PostAsJsonAsync("/api/benchmarks/run", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<BenchmarkComparisonResult>();
        Assert.NotNull(result);
        Assert.Equal("collection", result.BenchmarkId);
        Assert.Equal(3, result.Results.Count);
    }

    [Fact]
    public async Task RunComparison_InvalidBenchmarkId_ReturnsBadRequest()
    {
        // Arrange
        var request = new RunBenchmarkRequest("invalid_bench_999", Iterations: 100);

        // Act
        var response = await _client.PostAsJsonAsync("/api/benchmarks/run", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
