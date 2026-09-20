using Microsoft.OpenApi;
using SwaggerDocumentation.Api.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SwaggerDocumentation.Api.Filters;

/// <summary>
/// Tùy biến thông tin schema của các model Product trong tài liệu Swagger.
/// </summary>
public class ProductSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(CreateProductV1Request))
        {
            schema.Description = "Mô hình yêu cầu tạo sản phẩm phiên bản V1 (cơ bản).";
        }
        else if (context.Type == typeof(CreateProductV2Request))
        {
            schema.Description = "Mô hình yêu cầu tạo sản phẩm phiên bản V2 (nâng cao với SKU, Tags và Tồn kho).";
        }
    }
}
