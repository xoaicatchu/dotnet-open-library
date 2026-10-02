using WasmWasi.Api.Services;
using WasmWasi.Api.Wasm;

var builder = WebApplication.CreateBuilder(args);

// 1. Controllers
builder.Services.AddControllers();

// 2. Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "127 – WebAssembly + WASI with .NET",
        Version = "v1",
        Description = """
            API tích hợp WebAssembly trong .NET 10 qua thư viện Wasmtime (Bytecode Alliance).
            
            Khả năng:
            - Load WASM modules từ WAT text hoặc binary (.wasm)
            - Gọi exported functions với type-safe arguments
            - Fuel metering: ngăn infinite loop tự động
            - Upload WASM binary qua HTTP để đăng ký động
            
            Use cases:
            - Sandbox plugin execution (untrusted code an toàn)
            - Cross-language computation (Rust/C/Go compiled to WASM)
            - Edge computing & portable business rules
            """
    });
});

// 3. Wasmtime Engine (Singleton – Engine tốn kém, tái dùng qua requests)
builder.Services.AddSingleton<WasmEngineService>();

// 4. WASM services (Scoped – mỗi Store là độc lập)
builder.Services.AddScoped<IWasmInvoker, WasmInvoker>();
builder.Services.AddScoped<IWasmModuleRegistry, WasmModuleRegistry>();

var app = builder.Build();

// 5. Warm up: khởi tạo Engine ngay khi start (không lazy)
_ = app.Services.GetRequiredService<WasmEngineService>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();

public partial class Program { }
