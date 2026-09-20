using BenchmarkDotNet.Attributes;
using BenchmarkShowdown.Benchmarks.Models;
using FluentValidation;
using FluentValidation.Results;

namespace BenchmarkShowdown.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class ValidationBenchmark
{
    private CreateProductRequest _validRequest = null!;
    private CreateProductRequest _invalidRequest = null!;
    private IValidator<CreateProductRequest> _validator = null!;

    [GlobalSetup]
    public void Setup()
    {
        _validRequest = new CreateProductRequest("Product", "Desc", 99.99m, 10);
        _invalidRequest = new CreateProductRequest("", "", -1, -5);
        _validator = new CreateProductRequestValidator();
    }

    [Benchmark(Baseline = true)]
    public bool ManualValidation()
    {
        return !string.IsNullOrEmpty(_validRequest.Name)
            && _validRequest.Price > 0
            && _validRequest.Stock >= 0;
    }

    [Benchmark]
    public ValidationResult FluentValidation_Valid() => _validator.Validate(_validRequest);

    [Benchmark]
    public ValidationResult FluentValidation_Invalid() => _validator.Validate(_invalidRequest);
}
