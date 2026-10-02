using DynamicRuleEngine.Api.Domain.Interfaces;
using DynamicRuleEngine.Api.Infrastructure.Compilers;
using DynamicRuleEngine.Api.Infrastructure.Repositories;
using DynamicRuleEngine.Api.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Controllers
builder.Services.AddControllers();

// 2. Swagger UI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "125-Dynamic-RuleEngine API",
        Version = "v1",
        Description = "Động cơ Quy tắc Nghiệp vụ Động (.NET 10) - Hot-Reload, Compiled Expression Tree, BHYT & E-Commerce"
    });
});

// 3. Đăng ký các dịch vụ Dynamic Rule Engine
// 3.1. Trình biên dịch biểu thức logic (Singleton để tận dụng RAM cache)
builder.Services.AddSingleton<IRuleCompiler, ExpressionRuleCompiler>();

// 3.2. Kho lưu trữ Workflow & Rules (Singleton in-memory với Hot-Reload)
builder.Services.AddSingleton<IWorkflowRepository, InMemoryWorkflowRepository>();

// 3.3. Dịch vụ đánh giá luật nghiệp vụ
builder.Services.AddScoped<IRuleEvaluator, RuleEvaluatorService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();

// Hỗ trợ WebApplicationFactory trong Integration Tests
public partial class Program { }
