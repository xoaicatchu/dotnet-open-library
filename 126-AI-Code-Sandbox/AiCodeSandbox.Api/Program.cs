using AiCodeSandbox.Api.Ai;
using AiCodeSandbox.Api.Sandbox;
using AiCodeSandbox.Api.Services;
using Microsoft.SemanticKernel;

var builder = WebApplication.CreateBuilder(args);

// 1. Controllers
builder.Services.AddControllers();

// 2. Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "126 – AI Code Execution Sandbox",
        Version = "v1",
        Description = """
            Sandbox thực thi code với 3 chế độ:
            - InProcess  : Roslyn CSharpScript (nhanh, dev-only)
            - Docker     : Container cô lập (cần Docker daemon)
            - Wasi       : WebAssembly/WASI component (xem project 127)
            Tích hợp Semantic Kernel để sinh code từ ngôn ngữ tự nhiên.
            """
    });
});

// 3. Sandbox Executors
builder.Services.AddSingleton<ICodeExecutor, RoslynInProcessExecutor>();
builder.Services.AddSingleton<ICodeExecutor, DockerSandboxExecutor>();
builder.Services.AddScoped<ISandboxOrchestrator, SandboxOrchestrator>();

// 4. AI Code Generator via Semantic Kernel
// Nếu có OpenAI:ApiKey → dùng SK + OpenAI; nếu không → Stub (offline)
var openAiKey = builder.Configuration["OpenAI:ApiKey"]
    ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");

if (!string.IsNullOrEmpty(openAiKey))
{
    var kernelBuilder = builder.Services.AddKernel();
    kernelBuilder.AddOpenAIChatCompletion(
        modelId: builder.Configuration["OpenAI:ModelId"] ?? "gpt-4o-mini",
        apiKey: openAiKey);
    builder.Services.AddScoped<IAiCodeGenerator, SemanticKernelCodeGenerator>();
}
else
{
    // Không có API key → Stub (không cần mạng, luôn hoạt động)
    builder.Services.AddScoped<IAiCodeGenerator, StubAiCodeGenerator>();
}

var app = builder.Build();

// 5. Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();

public partial class Program { }
