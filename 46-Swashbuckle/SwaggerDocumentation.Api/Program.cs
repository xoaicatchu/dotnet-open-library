using System.Reflection;
using Microsoft.OpenApi;
using SwaggerDocumentation.Api.Filters;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    // Cấu hình tài liệu Swagger phiên bản V1
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Products API - V1",
        Version = "v1",
        Description = "API quản lý sản phẩm cơ bản phiên bản 1.0",
        Contact = new OpenApiContact
        {
            Name = "VNPT API Support Team",
            Email = "support-api@vnpt.vn",
            Url = new Uri("https://cntt.vnpt.vn")
        },
        License = new OpenApiLicense
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    // Cấu hình tài liệu Swagger phiên bản V2
    c.SwaggerDoc("v2", new OpenApiInfo
    {
        Title = "Products API - V2 (Enhanced)",
        Version = "v2",
        Description = "API quản lý sản phẩm nâng cao phiên bản 2.0 bổ sung SKU, tồn kho, đánh giá sao",
        Contact = new OpenApiContact
        {
            Name = "VNPT Architecture Team",
            Email = "arch@vnpt.vn"
        }
    });

    // Bật Swashbuckle Annotations ([SwaggerOperation], [SwaggerResponse])
    c.EnableAnnotations();

    // Tích hợp XML Comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Tích hợp Operation Filters & Schema Filters tùy biến
    c.OperationFilter<CorrelationIdOperationFilter>();
    c.SchemaFilter<ProductSchemaFilter>();

    // Cấu hình xác thực JWT Bearer trong Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nhập token JWT theo định dạng: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });

    c.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer"),
            new List<string>()
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Products API v1");
        c.SwaggerEndpoint("/swagger/v2/swagger.json", "Products API v2");
        c.RoutePrefix = "swagger";
        c.DisplayRequestDuration();
    });
}

app.MapControllers();

app.Run();

public partial class Program { }
