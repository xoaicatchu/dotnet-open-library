using AiCodeSandbox.Api.Models;
using AiCodeSandbox.Api.Sandbox;

namespace AiCodeSandbox.Api.Services;

/// <summary>
/// Điều phối việc chọn Executor phù hợp với (Language, ExecutionMode).
/// </summary>
public interface ISandboxOrchestrator
{
    Task<CodeExecutionResult> ExecuteAsync(CodeExecutionRequest request, CancellationToken ct = default);
}

public sealed class SandboxOrchestrator : ISandboxOrchestrator
{
    private readonly IEnumerable<ICodeExecutor> _executors;
    private readonly ILogger<SandboxOrchestrator> _logger;

    public SandboxOrchestrator(IEnumerable<ICodeExecutor> executors, ILogger<SandboxOrchestrator> logger)
    {
        _executors = executors;
        _logger = logger;
    }

    public async Task<CodeExecutionResult> ExecuteAsync(CodeExecutionRequest request, CancellationToken ct = default)
    {
        // Tìm executor phù hợp với mode được yêu cầu
        var executor = _executors.FirstOrDefault(e =>
            e.Mode == request.Mode && e.SupportsLanguage(request.Language));

        // Fallback: nếu không tìm được đúng mode, thử InProcess
        executor ??= _executors.FirstOrDefault(e =>
            e.Mode == ExecutionMode.InProcess && e.SupportsLanguage(request.Language));

        if (executor is null)
        {
            return new CodeExecutionResult(false, null,
                $"No executor found for language='{request.Language}' mode='{request.Mode}'",
                0, request.Mode, request.Language);
        }

        _logger.LogInformation("Executing {Language} code via {Mode} executor", request.Language, executor.Mode);

        return await executor.ExecuteAsync(request.Code, request.Language, request.TimeoutSeconds, ct);
    }
}
