using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SwaggerDocumentation.Api.Filters;

/// <summary>
/// Tự động bổ sung Header 'X-Correlation-Id' vào tất cả các endpoint trong tài liệu Swagger.
/// </summary>
public class CorrelationIdOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= [];

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-Correlation-Id",
            In = ParameterLocation.Header,
            Description = "Mã định danh tương quan phân tán (Distributed Tracing Correlation ID)",
            Required = false,
            Schema = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Format = "uuid"
            }
        });
    }
}
