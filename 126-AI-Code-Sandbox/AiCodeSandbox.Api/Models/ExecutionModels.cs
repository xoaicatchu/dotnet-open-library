namespace AiCodeSandbox.Api.Models;

// ── Execution request ──────────────────────────────────────────────────────

/// <summary>Yêu cầu thực thi code trực tiếp (không qua AI).</summary>
public record CodeExecutionRequest(
    string Code,
    string Language = "csharp",
    int TimeoutSeconds = 10,
    ExecutionMode Mode = ExecutionMode.InProcess
);

/// <summary>Yêu cầu tạo code từ mô tả tự nhiên, sau đó thực thi.</summary>
public record AiGenerateAndRunRequest(
    string Prompt,
    string Language = "csharp",
    int TimeoutSeconds = 10,
    ExecutionMode Mode = ExecutionMode.InProcess
);

/// <summary>Chỉ tạo code từ AI, không thực thi.</summary>
public record AiGenerateCodeRequest(string Prompt, string Language = "csharp");

// ── Execution result ───────────────────────────────────────────────────────

public record CodeExecutionResult(
    bool Success,
    string? Output,
    string? Error,
    long ElapsedMs,
    ExecutionMode Mode,
    string Language
);

public record AiCodeGenerationResult(
    string GeneratedCode,
    string Language,
    string? Explanation
);

// ── Enums ──────────────────────────────────────────────────────────────────

/// <summary>
/// InProcess  = Roslyn CSharpScript (nhanh, ít cô lập).
/// Docker     = Container sandbox (chậm hơn, cô lập hoàn toàn).
/// Wasi       = WebAssembly/WASI component (cô lập tốt, đa ngôn ngữ).
/// </summary>
public enum ExecutionMode { InProcess, Docker, Wasi }
