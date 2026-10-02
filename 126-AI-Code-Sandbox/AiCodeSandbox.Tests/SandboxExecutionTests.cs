using System.Net;
using System.Net.Http.Json;
using AiCodeSandbox.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AiCodeSandbox.Tests;

public class SandboxExecutionTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SandboxExecutionTests(WebApplicationFactory<Program> factory)
        => _client = factory.CreateClient();

    // ── InProcess / Roslyn ─────────────────────────────────────────────────

    [Fact]
    public async Task Execute_SimpleConsoleWrite_ReturnsOutput()
    {
        var request = new CodeExecutionRequest(
            Code: "Console.WriteLine(\"Hello Sandbox!\");",
            Language: "csharp",
            TimeoutSeconds: 5,
            Mode: ExecutionMode.InProcess);

        var response = await _client.PostAsJsonAsync("/api/sandbox/execute", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CodeExecutionResult>();
        Assert.NotNull(result);
        Assert.True(result!.Success);
        Assert.Contains("Hello Sandbox!", result.Output ?? "");
    }

    [Fact]
    public async Task Execute_MathCalculation_ReturnsCorrectResult()
    {
        var code = """
            var sum = Enumerable.Range(1, 100).Sum();
            Console.WriteLine(sum);
            """;
        var request = new CodeExecutionRequest(code, "csharp", 5, ExecutionMode.InProcess);

        var response = await _client.PostAsJsonAsync("/api/sandbox/execute", request);
        var result = await response.Content.ReadFromJsonAsync<CodeExecutionResult>();

        Assert.True(result!.Success);
        Assert.Contains("5050", result.Output ?? "");
    }

    [Fact]
    public async Task Execute_RuntimeException_ReturnsFalseWithError()
    {
        var request = new CodeExecutionRequest(
            Code: "throw new InvalidOperationException(\"test error\");",
            Language: "csharp",
            TimeoutSeconds: 5,
            Mode: ExecutionMode.InProcess);

        var response = await _client.PostAsJsonAsync("/api/sandbox/execute", request);
        var result = await response.Content.ReadFromJsonAsync<CodeExecutionResult>();

        Assert.NotNull(result);
        Assert.False(result!.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task Execute_DivisionByZero_ReturnsFalseWithError()
    {
        // Verify runtime exception handling — divide by zero
        var request = new CodeExecutionRequest(
            Code: "int result = 10 / 0; Console.WriteLine(result);",
            Language: "csharp",
            TimeoutSeconds: 5,
            Mode: ExecutionMode.InProcess);

        var response = await _client.PostAsJsonAsync("/api/sandbox/execute", request);
        var result = await response.Content.ReadFromJsonAsync<CodeExecutionResult>();

        Assert.NotNull(result);
        Assert.False(result!.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task Execute_EmptyCode_ReturnsBadRequest()
    {
        var request = new CodeExecutionRequest(Code: "   ", Language: "csharp");

        var response = await _client.PostAsJsonAsync("/api/sandbox/execute", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── AI Code Generation (Stub) ──────────────────────────────────────────

    [Fact]
    public async Task GenerateCode_WithPrompt_ReturnsNonEmptyCode()
    {
        var request = new AiGenerateCodeRequest("in Hello World", "csharp");

        var response = await _client.PostAsJsonAsync("/api/ai/generate", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AiCodeGenerationResult>();
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result!.GeneratedCode));
        Assert.Equal("csharp", result.Language);
    }

    [Fact]
    public async Task GenerateAndRun_WithStub_ReturnsGeneratedAndExecutionResult()
    {
        var request = new AiGenerateAndRunRequest(
            Prompt: "tính Fibonacci",
            Language: "csharp",
            TimeoutSeconds: 10,
            Mode: ExecutionMode.InProcess);

        var response = await _client.PostAsJsonAsync("/api/ai/generate-and-run", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("generated", json);
        Assert.Contains("execution", json);
    }

    // ── Performance sanity ─────────────────────────────────────────────────

    [Fact]
    public async Task Execute_SimpleCode_CompletesUnder2Seconds()
    {
        var request = new CodeExecutionRequest(
            "Console.WriteLine(42);", "csharp", 5, ExecutionMode.InProcess);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        await _client.PostAsJsonAsync("/api/sandbox/execute", request);
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 2000,
            $"Expected < 2000ms but took {sw.ElapsedMilliseconds}ms");
    }
}
