using Microsoft.AspNetCore.Mvc;
using WasmWasi.Api.Models;
using WasmWasi.Api.Services;
using WasmWasi.Api.Wasm;

namespace WasmWasi.Api.Controllers;

/// <summary>
/// Quản lý WASM modules: list, upload.
/// GET  /api/modules
/// GET  /api/modules/{name}
/// POST /api/modules/upload
/// </summary>
[ApiController]
[Route("api/modules")]
public class ModulesController : ControllerBase
{
    private readonly IWasmModuleRegistry _registry;

    public ModulesController(IWasmModuleRegistry registry) => _registry = registry;

    /// <summary>Liệt kê tất cả WASM module đã được load.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<WasmModuleInfo>>(StatusCodes.Status200OK)]
    public IActionResult ListModules() => Ok(_registry.ListModules());

    /// <summary>Thông tin chi tiết một module.</summary>
    [HttpGet("{name}")]
    [ProducesResponseType<WasmModuleInfo>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetModule(string name)
    {
        var info = _registry.GetModule(name);
        return info is null ? NotFound(new { error = $"Module '{name}' not found." }) : Ok(info);
    }

    /// <summary>
    /// Upload WASM binary (.wasm file) và đăng ký vào registry.
    /// Content-Type: application/wasm
    /// </summary>
    [HttpPost("upload")]
    [ProducesResponseType<WasmModuleInfo>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Consumes("application/octet-stream")]
    public async Task<IActionResult> UploadModule([FromQuery] string name, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { error = "Tham số 'name' là bắt buộc." });

        using var ms = new MemoryStream();
        await Request.Body.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();

        if (bytes.Length == 0)
            return BadRequest(new { error = "Request body không chứa WASM binary." });

        // Kiểm tra magic bytes: \0asm
        if (bytes.Length < 4 || bytes[0] != 0x00 || bytes[1] != 0x61 || bytes[2] != 0x73 || bytes[3] != 0x6D)
            return BadRequest(new { error = "File không phải WASM binary hợp lệ (thiếu magic bytes \\0asm)." });

        try
        {
            var info = _registry.RegisterFromBytes(name, bytes);
            return Ok(info);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Không thể parse WASM: {ex.Message}" });
        }
    }
}

/// <summary>
/// Gọi hàm trong WASM module.
/// POST /api/invoke
/// </summary>
[ApiController]
[Route("api/invoke")]
public class InvokeController : ControllerBase
{
    private readonly IWasmInvoker _invoker;

    public InvokeController(IWasmInvoker invoker) => _invoker = invoker;

    /// <summary>
    /// Gọi một exported function trong WASM module.
    /// Args có thể là I32, I64, F32, F64 hoặc String.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<WasmInvokeResult>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Invoke([FromBody] WasmInvokeRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ModuleName) || string.IsNullOrWhiteSpace(request.FunctionName))
            return BadRequest(new { error = "ModuleName và FunctionName là bắt buộc." });

        var result = await _invoker.InvokeAsync(request, ct);
        return Ok(result);
    }
}

/// <summary>
/// Thông tin về runtime WASI/Wasmtime.
/// GET /api/runtime
/// </summary>
[ApiController]
[Route("api/runtime")]
public class RuntimeController : ControllerBase
{
    private readonly WasmEngineService _engine;

    public RuntimeController(WasmEngineService engine) => _engine = engine;

    [HttpGet]
    public IActionResult GetRuntimeInfo() => Ok(new
    {
        runtime = "Wasmtime (Bytecode Alliance)",
        dotnetVersion = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
        architecture = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString(),
        loadedModules = _engine.Registry.Keys.ToList(),
        features = new[] { "WASM MVP", "WASI Preview 1", "Fuel metering (infinite-loop protection)" }
    });
}
