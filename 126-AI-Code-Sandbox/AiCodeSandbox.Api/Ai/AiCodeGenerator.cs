using AiCodeSandbox.Api.Models;
using Microsoft.SemanticKernel;

namespace AiCodeSandbox.Api.Ai;

/// <summary>
/// Gọi LLM qua Semantic Kernel để sinh code từ mô tả tự nhiên.
/// Hỗ trợ OpenAI và Azure OpenAI provider (cùng pattern với 119-RAG-Pipeline).
/// </summary>
public interface IAiCodeGenerator
{
    Task<AiCodeGenerationResult> GenerateAsync(string prompt, string language,
        CancellationToken ct = default);
}

public sealed class SemanticKernelCodeGenerator : IAiCodeGenerator
{
    private readonly Kernel _kernel;
    private readonly ILogger<SemanticKernelCodeGenerator> _logger;

    public SemanticKernelCodeGenerator(Kernel kernel, ILogger<SemanticKernelCodeGenerator> logger)
    {
        _kernel = kernel;
        _logger = logger;
    }

    public async Task<AiCodeGenerationResult> GenerateAsync(string prompt, string language,
        CancellationToken ct = default)
    {
        var skPrompt = $"""
            You are an expert {language} developer.
            When asked to write code, respond with ONLY the code (no markdown fences, no explanation).
            The code must be self-contained and use Console.WriteLine for output.
            Keep it concise – under 50 lines.

            Write {language} code that: {prompt}
            """;

        _logger.LogInformation("Calling Semantic Kernel to generate {Language} code for: {Prompt}", language, prompt);

        var result = await _kernel.InvokePromptAsync(skPrompt, cancellationToken: ct);
        var generatedCode = result.GetValue<string>() ?? string.Empty;

        return new AiCodeGenerationResult(generatedCode, language,
            $"Generated {language} code for: {prompt}");
    }
}

/// <summary>
/// Stub dùng khi không có API key – trả về code mẫu cứng cho demo.
/// Hoạt động hoàn toàn offline, không cần internet.
/// </summary>
public sealed class StubAiCodeGenerator : IAiCodeGenerator
{
    public Task<AiCodeGenerationResult> GenerateAsync(string prompt, string language,
        CancellationToken ct = default)
    {
        var stubCode = language.ToLower() switch
        {
            "csharp" or "cs" => $$"""
                // Auto-generated stub for: {{prompt}}
                var fibonacci = new List<long> { 0, 1 };
                for (int i = 2; i < 10; i++)
                    fibonacci.Add(fibonacci[i - 1] + fibonacci[i - 2]);
                Console.WriteLine("Fibonacci: " + string.Join(", ", fibonacci));
                """,
            "python" => $"# Auto-generated stub for: {prompt}\nprint('Hello from Python stub!')",
            _ => $"// stub for {language}: {prompt}"
        };

        return Task.FromResult(new AiCodeGenerationResult(stubCode, language,
            "Stub generator – set OpenAI:ApiKey in appsettings.json to use real AI"));
    }
}
