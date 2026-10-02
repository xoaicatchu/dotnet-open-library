using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using WasmWasi.Api.Models;

namespace WasmWasi.Tests;

public class WasmInvokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public WasmInvokeTests(WebApplicationFactory<Program> factory)
        => _client = factory.CreateClient();

    // ── Module registry ────────────────────────────────────────────────────

    [Fact]
    public async Task ListModules_ReturnsAtLeastMathDemoModule()
    {
        var response = await _client.GetAsync("/api/modules");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var modules = await response.Content.ReadFromJsonAsync<List<WasmModuleInfo>>();
        Assert.NotNull(modules);
        Assert.Contains(modules!, m => m.Name == "math-demo");
    }

    [Fact]
    public async Task GetModule_MathDemo_ReturnsExportedFunctions()
    {
        var response = await _client.GetAsync("/api/modules/math-demo");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var info = await response.Content.ReadFromJsonAsync<WasmModuleInfo>();
        Assert.NotNull(info);
        Assert.Contains("add", info!.ExportedFunctions);
        Assert.Contains("multiply", info.ExportedFunctions);
        Assert.Contains("factorial", info.ExportedFunctions);
    }

    [Fact]
    public async Task GetModule_Unknown_Returns404()
    {
        var response = await _client.GetAsync("/api/modules/does-not-exist");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── Function invocation ────────────────────────────────────────────────

    [Fact]
    public async Task Invoke_Add_ReturnsCorrectSum()
    {
        var request = new WasmInvokeRequest("math-demo", "add",
        [
            new WasmValue(WasmValueKind.I32, "10"),
            new WasmValue(WasmValueKind.I32, "32")
        ]);

        var response = await _client.PostAsJsonAsync("/api/invoke", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<WasmInvokeResult>();
        Assert.NotNull(result);
        Assert.True(result!.Success);
        Assert.Contains("42", result.ReturnValue ?? "");
    }

    [Fact]
    public async Task Invoke_Multiply_ReturnsCorrectProduct()
    {
        var request = new WasmInvokeRequest("math-demo", "multiply",
        [
            new WasmValue(WasmValueKind.I32, "7"),
            new WasmValue(WasmValueKind.I32, "6")
        ]);

        var response = await _client.PostAsJsonAsync("/api/invoke", request);
        var result = await response.Content.ReadFromJsonAsync<WasmInvokeResult>();

        Assert.True(result!.Success);
        Assert.Contains("42", result.ReturnValue ?? "");
    }

    [Fact]
    public async Task Invoke_Factorial10_Returns3628800()
    {
        var request = new WasmInvokeRequest("math-demo", "factorial",
        [
            new WasmValue(WasmValueKind.I32, "10")
        ]);

        var response = await _client.PostAsJsonAsync("/api/invoke", request);
        var result = await response.Content.ReadFromJsonAsync<WasmInvokeResult>();

        Assert.True(result!.Success);
        Assert.Contains("3628800", result.ReturnValue ?? "");
    }

    [Fact]
    public async Task Invoke_NonExistentFunction_ReturnsFalseWithError()
    {
        var request = new WasmInvokeRequest("math-demo", "doesNotExist", []);

        var response = await _client.PostAsJsonAsync("/api/invoke", request);
        var result = await response.Content.ReadFromJsonAsync<WasmInvokeResult>();

        Assert.NotNull(result);
        Assert.False(result!.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task Invoke_NonExistentModule_ReturnsFalseWithHelpfulMessage()
    {
        var request = new WasmInvokeRequest("ghost-module", "add",
        [
            new WasmValue(WasmValueKind.I32, "1"),
            new WasmValue(WasmValueKind.I32, "2")
        ]);

        var response = await _client.PostAsJsonAsync("/api/invoke", request);
        var result = await response.Content.ReadFromJsonAsync<WasmInvokeResult>();

        Assert.False(result!.Success);
        Assert.Contains("math-demo", result.Error ?? ""); // error message lists available modules
    }

    // ── Runtime info ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetRuntime_ReturnsWasmtimeInfo()
    {
        var response = await _client.GetAsync("/api/runtime");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("Wasmtime", json);
        Assert.Contains("math-demo", json);
    }

    // ── Performance ────────────────────────────────────────────────────────

    [Fact]
    public async Task Invoke_Add_CompletesUnder500ms()
    {
        var request = new WasmInvokeRequest("math-demo", "add",
        [
            new WasmValue(WasmValueKind.I32, "100"),
            new WasmValue(WasmValueKind.I32, "200")
        ]);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        await _client.PostAsJsonAsync("/api/invoke", request);
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 500,
            $"Expected < 500ms but took {sw.ElapsedMilliseconds}ms");
    }
}
