using AiCodeSandbox.Api.Models;

namespace AiCodeSandbox.Api.Sandbox;

// ── Abstraction ────────────────────────────────────────────────────────────

/// <summary>Giao diện thực thi code trong sandbox.</summary>
public interface ICodeExecutor
{
    ExecutionMode Mode { get; }
    bool SupportsLanguage(string language);
    Task<CodeExecutionResult> ExecuteAsync(string code, string language, int timeoutSeconds,
        CancellationToken ct = default);
}

// ── In-Process Roslyn Executor ─────────────────────────────────────────────

/// <summary>
/// Thực thi C# bằng Roslyn CSharpScript trong cùng process.
/// Nhanh (~50ms), phù hợp dev/demo. KHÔNG dùng cho untrusted input production.
/// </summary>
public sealed class RoslynInProcessExecutor : ICodeExecutor
{
    public ExecutionMode Mode => ExecutionMode.InProcess;

    public bool SupportsLanguage(string language) =>
        language.Equals("csharp", StringComparison.OrdinalIgnoreCase) ||
        language.Equals("cs", StringComparison.OrdinalIgnoreCase);

    public async Task<CodeExecutionResult> ExecuteAsync(
        string code, string language, int timeoutSeconds, CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Capture Console output
        var originalOut = Console.Out;
        using var buffer = new System.IO.StringWriter();
        Console.SetOut(buffer);

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            // ScriptOptions: reference core assemblies + import namespaces
            var scriptOptions = Microsoft.CodeAnalysis.Scripting.ScriptOptions.Default
                .WithReferences(
                    typeof(object).Assembly,                        // System.Private.CoreLib
                    typeof(Enumerable).Assembly,                    // System.Linq
                    typeof(System.Threading.Tasks.Task).Assembly,  // System.Threading.Tasks (same as CoreLib on net10)
                    typeof(System.Collections.Generic.List<>).Assembly,
                    typeof(System.Text.StringBuilder).Assembly,
                    typeof(Console).Assembly)
                .WithImports(
                    "System",
                    "System.Linq",
                    "System.Collections.Generic",
                    "System.Threading",
                    "System.Threading.Tasks",
                    "System.Text");

            await Microsoft.CodeAnalysis.CSharp.Scripting
                .CSharpScript.RunAsync(code,
                    options: scriptOptions,
                    cancellationToken: cts.Token);

            sw.Stop();
            return new CodeExecutionResult(true, buffer.ToString(), null, sw.ElapsedMilliseconds, Mode, language);
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            return new CodeExecutionResult(false, null,
                $"Execution timed out after {timeoutSeconds}s", sw.ElapsedMilliseconds, Mode, language);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new CodeExecutionResult(false, buffer.ToString(), ex.Message, sw.ElapsedMilliseconds, Mode, language);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}

// ── Docker Executor (stub – cần Docker daemon) ─────────────────────────────

/// <summary>
/// Thực thi code trong Docker container cô lập.
/// Yêu cầu Docker daemon đang chạy trên host.
/// </summary>
public sealed class DockerSandboxExecutor : ICodeExecutor
{
    private readonly ILogger<DockerSandboxExecutor> _logger;

    public DockerSandboxExecutor(ILogger<DockerSandboxExecutor> logger) => _logger = logger;

    public ExecutionMode Mode => ExecutionMode.Docker;

    public bool SupportsLanguage(string language) =>
        language is "csharp" or "cs" or "python" or "javascript" or "js";

    public async Task<CodeExecutionResult> ExecuteAsync(
        string code, string language, int timeoutSeconds, CancellationToken ct = default)
    {
        // TODO: Implement full Docker sandbox
        // 1. Chọn image: mcr.microsoft.com/dotnet/sdk:10.0 | python:3.12-alpine | node:22-alpine
        // 2. Tạo temp container với --memory 128m --cpus 0.5 --network none --read-only
        // 3. Copy code vào /app/script.cs (hoặc .py, .js)
        // 4. docker exec: dotnet-script / python / node
        // 5. Capture stdout/stderr, kill sau timeout, rm container
        _logger.LogWarning("DockerSandboxExecutor: not yet fully implemented – returning stub result");
        await Task.Delay(50, ct);
        return new CodeExecutionResult(false, null,
            "Docker sandbox not yet implemented. Ensure Docker daemon is running.",
            50, Mode, language);
    }
}
