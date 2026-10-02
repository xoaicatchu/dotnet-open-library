using Wasmtime;
using WasmWasi.Api.Models;
using WasmWasi.Api.Wasm;

namespace WasmWasi.Api.Services;

/// <summary>
/// Gọi function trong WASM module đã đăng ký.
/// Wasmtime v34: Store không generic, dùng instance.GetFunction(store, name).
/// </summary>
public interface IWasmInvoker
{
    Task<WasmInvokeResult> InvokeAsync(WasmInvokeRequest request, CancellationToken ct = default);
}

public sealed class WasmInvoker : IWasmInvoker
{
    private readonly WasmEngineService _engine;
    private readonly ILogger<WasmInvoker> _logger;

    public WasmInvoker(WasmEngineService engine, ILogger<WasmInvoker> logger)
    {
        _engine = engine;
        _logger = logger;
    }

    public Task<WasmInvokeResult> InvokeAsync(WasmInvokeRequest request, CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        if (!_engine.Registry.TryGetValue(request.ModuleName, out var entry))
        {
            return Task.FromResult(new WasmInvokeResult(false, null,
                $"Module '{request.ModuleName}' not found. Available: {string.Join(", ", _engine.Registry.Keys)}",
                0, request.ModuleName, request.FunctionName));
        }

        try
        {
            // Wasmtime v34: Store không generic
            using var store = _engine.CreateStore();
            using var linker = new Linker(_engine.Engine);
            var instance = linker.Instantiate(store, entry.Module);

            // Wasmtime v34: GetFunction chỉ nhận tên, không nhận Store
            var fn = instance.GetFunction(request.FunctionName);
            if (fn is null)
            {
                // List available exports for helpful error message
                var exports = entry.Module.Exports.Select(e => e.Name).ToList();
                return Task.FromResult(new WasmInvokeResult(false, null,
                    $"Function '{request.FunctionName}' not exported. Available: {string.Join(", ", exports)}",
                    0, request.ModuleName, request.FunctionName));
            }

            // Chuyển đổi args sang ValueBox (có implicit conversion từ int/long/float/double/string)
            var args = request.Args.Select(MapToValueBox).ToArray();

            // Invoke với ValueBox[]
            var returnValues = fn.Invoke(args);
            sw.Stop();

            var returnStr = returnValues is null ? "void"
                : returnValues is object[] arr ? string.Join(", ", arr.Select(v => v?.ToString() ?? "null"))
                : returnValues.ToString() ?? "null";

            _logger.LogInformation("WASM {Module}.{Fn}({Args}) = {Return} in {Ms}ms",
                request.ModuleName, request.FunctionName,
                string.Join(", ", args), returnStr, sw.ElapsedMilliseconds);

            return Task.FromResult(new WasmInvokeResult(true, returnStr, null,
                sw.ElapsedMilliseconds, request.ModuleName, request.FunctionName));
        }
        catch (Exception ex)
        {
            sw.Stop();
            return Task.FromResult(new WasmInvokeResult(false, null, ex.Message,
                sw.ElapsedMilliseconds, request.ModuleName, request.FunctionName));
        }
    }

    private static Wasmtime.ValueBox MapToValueBox(WasmValue v) => v.Kind switch
    {
        WasmValueKind.I32 => (Wasmtime.ValueBox)int.Parse(v.Value),
        WasmValueKind.I64 => (Wasmtime.ValueBox)long.Parse(v.Value),
        WasmValueKind.F32 => (Wasmtime.ValueBox)float.Parse(v.Value),
        WasmValueKind.F64 => (Wasmtime.ValueBox)double.Parse(v.Value),
        _ => (Wasmtime.ValueBox)v.Value  // String implicit conversion
    };
}

/// <summary>Quản lý module registry: list, upload.</summary>
public interface IWasmModuleRegistry
{
    IReadOnlyList<WasmModuleInfo> ListModules();
    WasmModuleInfo? GetModule(string name);
    WasmModuleInfo RegisterFromBytes(string name, byte[] wasmBytes);
}

public sealed class WasmModuleRegistry : IWasmModuleRegistry
{
    private readonly WasmEngineService _engine;

    public WasmModuleRegistry(WasmEngineService engine) => _engine = engine;

    public IReadOnlyList<WasmModuleInfo> ListModules() =>
        _engine.Registry.Select(kv => new WasmModuleInfo(
            kv.Key,
            $"WASM module: {kv.Key}",
            kv.Value.Module.Exports.Select(e => e.Name).ToList(),
            kv.Value.Bytes.Length
        )).ToList();

    public WasmModuleInfo? GetModule(string name)
    {
        if (!_engine.Registry.TryGetValue(name, out var entry)) return null;
        return new WasmModuleInfo(name, $"WASM module: {name}",
            entry.Module.Exports.Select(e => e.Name).ToList(), entry.Bytes.Length);
    }

    public WasmModuleInfo RegisterFromBytes(string name, byte[] wasmBytes) =>
        _engine.RegisterModule(name, wasmBytes);
}
