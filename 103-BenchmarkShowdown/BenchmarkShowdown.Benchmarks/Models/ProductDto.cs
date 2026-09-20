namespace BenchmarkShowdown.Benchmarks.Models;

public record ProductDto(int Id, string Name, decimal Price, int Stock)
{
    public ProductDto() : this(0, string.Empty, 0, 0) { }
}
