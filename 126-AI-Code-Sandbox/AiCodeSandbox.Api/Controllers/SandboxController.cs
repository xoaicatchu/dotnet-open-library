using AiCodeSandbox.Api.Ai;
using AiCodeSandbox.Api.Models;
using AiCodeSandbox.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AiCodeSandbox.Api.Controllers;

/// <summary>
/// Thực thi code trực tiếp (không qua AI).
/// POST /api/sandbox/execute
/// </summary>
[ApiController]
[Route("api/sandbox")]
public class SandboxController : ControllerBase
{
    private readonly ISandboxOrchestrator _orchestrator;

    public SandboxController(ISandboxOrchestrator orchestrator) => _orchestrator = orchestrator;

    /// <summary>Thực thi đoạn code được cung cấp trong sandbox.</summary>
    [HttpPost("execute")]
    [ProducesResponseType<CodeExecutionResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Execute([FromBody] CodeExecutionRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(new { error = "Code không được để trống." });

        var result = await _orchestrator.ExecuteAsync(request, ct);
        return Ok(result);
    }
}

/// <summary>
/// Tích hợp AI: sinh code từ prompt rồi (tùy chọn) thực thi.
/// POST /api/ai/generate
/// POST /api/ai/generate-and-run
/// </summary>
[ApiController]
[Route("api/ai")]
public class AiController : ControllerBase
{
    private readonly IAiCodeGenerator _generator;
    private readonly ISandboxOrchestrator _orchestrator;

    public AiController(IAiCodeGenerator generator, ISandboxOrchestrator orchestrator)
    {
        _generator = generator;
        _orchestrator = orchestrator;
    }

    /// <summary>Chỉ sinh code từ mô tả (không chạy).</summary>
    [HttpPost("generate")]
    [ProducesResponseType<AiCodeGenerationResult>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Generate([FromBody] AiGenerateCodeRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
            return BadRequest(new { error = "Prompt không được để trống." });

        var result = await _generator.GenerateAsync(request.Prompt, request.Language, ct);
        return Ok(result);
    }

    /// <summary>Sinh code từ AI rồi thực thi ngay trong sandbox.</summary>
    [HttpPost("generate-and-run")]
    [ProducesResponseType<object>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateAndRun([FromBody] AiGenerateAndRunRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
            return BadRequest(new { error = "Prompt không được để trống." });

        // Bước 1: AI sinh code
        var generated = await _generator.GenerateAsync(request.Prompt, request.Language, ct);

        // Bước 2: Thực thi code vừa sinh
        var execRequest = new CodeExecutionRequest(generated.GeneratedCode, request.Language,
            request.TimeoutSeconds, request.Mode);
        var execResult = await _orchestrator.ExecuteAsync(execRequest, ct);

        return Ok(new
        {
            generated,
            execution = execResult
        });
    }
}
