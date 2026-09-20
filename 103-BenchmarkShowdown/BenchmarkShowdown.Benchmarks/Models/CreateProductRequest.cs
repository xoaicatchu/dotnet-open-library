namespace BenchmarkShowdown.Benchmarks.Models;

public record CreateProductRequest(string Name, string Desc, decimal Price, int Stock);
