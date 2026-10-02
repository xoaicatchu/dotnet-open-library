namespace WasmWasi.Api.Models;

// ── WASM Module management ─────────────────────────────────────────────────

/// <summary>Metadata về một WASM module được load.</summary>
public record WasmModuleInfo(
    string Name,
    string Description,
    IReadOnlyList<string> ExportedFunctions,
    long SizeBytes
);

// ── Function invocation ────────────────────────────────────────────────────

/// <summary>Yêu cầu gọi một hàm trong WASM module.</summary>
public record WasmInvokeRequest(
    string ModuleName,
    string FunctionName,
    IReadOnlyList<WasmValue> Args
);

public record WasmValue(WasmValueKind Kind, string Value);

public enum WasmValueKind { I32, I64, F32, F64, String }

/// <summary>Kết quả gọi hàm WASM.</summary>
public record WasmInvokeResult(
    bool Success,
    string? ReturnValue,
    string? Error,
    long ElapsedMs,
    string ModuleName,
    string FunctionName
);

// ── WASI sandbox execution ─────────────────────────────────────────────────

/// <summary>Chạy một WASM component với stdin input (WASI).</summary>
public record WasiRunRequest(
    string ModuleName,
    string? StdinInput = null,
    IDictionary<string, string>? EnvironmentVars = null,
    int TimeoutSeconds = 10
);

public record WasiRunResult(
    bool Success,
    string? Stdout,
    string? Stderr,
    int ExitCode,
    long ElapsedMs
);
