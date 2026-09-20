using NswagToolchain.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Cấu hình NSwag OpenAPI Document Generator
builder.Services.AddOpenApiDocument(config =>
{
    config.DocumentName = "v1";
    config.Title = "Project Tasks API";
    config.Version = "v1";
    config.Description = "API quản lý tiến độ công việc dự án và tự động sinh mã nguồn Client SDK (C# / TypeScript).";
});

builder.Services.AddSingleton<ITaskService, TaskService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Phục vụ OpenAPI JSON Spec tại /swagger/v1/swagger.json
    app.UseOpenApi();

    // Phục vụ Swagger UI tại /swagger
    app.UseSwaggerUi(settings =>
    {
        settings.Path = "/swagger";
    });

    // Phục vụ ReDoc UI tại /redoc
    app.UseReDoc(settings =>
    {
        settings.Path = "/redoc";
    });
}

app.MapControllers();

app.Run();

public partial class Program { }
