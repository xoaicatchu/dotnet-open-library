using Wasmtime;
using WasmWasi.Api.Models;

namespace WasmWasi.Api.Wasm;

/// <summary>
/// Quản lý vòng đời Wasmtime Engine và Module registry.
/// Singleton – Engine được tái dùng qua nhiều request.
/// Wasmtime v34: Store không còn generic, dùng Store(engine).
/// </summary>
public sealed class WasmEngineService : IDisposable
{
    private readonly Engine _engine;
    private readonly Dictionary<string, (Module Module, byte[] Bytes)> _registry = new();
    private readonly ILogger<WasmEngineService> _logger;

    public WasmEngineService(ILogger<WasmEngineService> logger)
    {
        _logger = logger;
        _engine = new Engine();

        // Load bundled demo modules khi khởi động
        LoadDemoModules();
    }

    // ── Module registry ────────────────────────────────────────────────────

    public IReadOnlyDictionary<string, (Module Module, byte[] Bytes)> Registry => _registry;

    public Engine Engine => _engine;

    /// <summary>Load WASM module từ byte array và đăng ký với tên.</summary>
    public WasmModuleInfo RegisterModule(string name, byte[] wasmBytes)
    {
        var module = Module.FromBytes(_engine, name, wasmBytes);
        _registry[name] = (module, wasmBytes);

        var exports = module.Exports.Select(e => e.Name).ToList();
        _logger.LogInformation("Registered WASM module '{Name}' ({Size} bytes, {Count} exports)",
            name, wasmBytes.Length, exports.Count);

        return new WasmModuleInfo(name, $"WASM module: {name}", exports, wasmBytes.Length);
    }

    /// <summary>Tạo Store mới cho mỗi execution (Wasmtime v34: Store không generic).</summary>
    public Store CreateStore() => new Store(_engine);

    // ── Demo modules ───────────────────────────────────────────────────────

    private void LoadDemoModules()
    {
        var mathWat = """
            (module
              (func (export "add") (param i32 i32) (result i32)
                local.get 0
                local.get 1
                i32.add)
              (func (export "multiply") (param i32 i32) (result i32)
                local.get 0
                local.get 1
                i32.mul)
              (func (export "factorial") (param i32) (result i32)
                (local i32)
                i32.const 1
                local.set 1
                block $break
                  loop $loop
                    local.get 0
                    i32.const 1
                    i32.le_s
                    br_if $break
                    local.get 1
                    local.get 0
                    i32.mul
                    local.set 1
                    local.get 0
                    i32.const 1
                    i32.sub
                    local.set 0
                    br $loop
                  end
                end
                local.get 1)
            )
            """;

        try
        {
            var module = Module.FromText(_engine, "math-demo", mathWat);
            // Serialize để lấy bytes cho WasmModuleInfo
            var bytes = module.Serialize();
            _registry["math-demo"] = (module, bytes);
            _logger.LogInformation("Loaded built-in WASM demo module 'math-demo' (add, multiply, factorial)");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load demo WASM module");
        }
    }

    public void Dispose() => _engine.Dispose();
}
