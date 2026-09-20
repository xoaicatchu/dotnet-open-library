using Microsoft.AspNetCore.Mvc;
using NSwag;
using NSwag.Annotations;
using NSwag.CodeGeneration.CSharp;
using NSwag.CodeGeneration.TypeScript;
using NswagToolchain.Api.Models;

namespace NswagToolchain.Api.Controllers;

/// <summary>
/// Trình tạo mã nguồn Client SDK tự động (C# / TypeScript) dựa trên tài liệu OpenAPI của NSwag.
/// </summary>
[ApiController]
[Route("api/client-gen")]
[Produces("application/json")]
[OpenApiTag("CodeGeneration", Description = "Sinh mã nguồn Client SDK tự động")]
public class ClientGeneratorController : ControllerBase
{
    private readonly IServiceProvider _serviceProvider;

    public ClientGeneratorController(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Sinh mã nguồn C# HttpClient SDK hoàn chỉnh từ đặc tả OpenAPI hiện tại.
    /// </summary>
    [HttpGet("csharp")]
    [ProducesResponseType(typeof(GeneratedClientResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateCSharpClient()
    {
        var document = await GetCurrentOpenApiDocumentAsync();
        var settings = new CSharpClientGeneratorSettings
        {
            ClassName = "TasksClient",
            CSharpGeneratorSettings =
            {
                Namespace = "ProjectManagement.Client"
            }
        };

        var generator = new CSharpClientGenerator(document, settings);
        var code = generator.GenerateFile();

        return Ok(new GeneratedClientResponse("CSharp", code, DateTime.UtcNow));
    }

    /// <summary>
    /// Sinh mã nguồn TypeScript (Fetch API) SDK hoàn chỉnh từ đặc tả OpenAPI hiện tại.
    /// </summary>
    [HttpGet("typescript")]
    [ProducesResponseType(typeof(GeneratedClientResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateTypeScriptClient()
    {
        var document = await GetCurrentOpenApiDocumentAsync();
        var settings = new TypeScriptClientGeneratorSettings
        {
            ClassName = "TasksClient",
            Template = TypeScriptTemplate.Fetch
        };

        var generator = new TypeScriptClientGenerator(document, settings);
        var code = generator.GenerateFile();

        return Ok(new GeneratedClientResponse("TypeScript", code, DateTime.UtcNow));
    }

    private async Task<OpenApiDocument> GetCurrentOpenApiDocumentAsync()
    {
        var settings = new NSwag.Generation.AspNetCore.AspNetCoreOpenApiDocumentGeneratorSettings
        {
            DocumentName = "v1",
            Title = "Project Tasks API"
        };
        var generator = new NSwag.Generation.AspNetCore.AspNetCoreOpenApiDocumentGenerator(settings);
        return await generator.GenerateAsync(_serviceProvider);
    }
}
