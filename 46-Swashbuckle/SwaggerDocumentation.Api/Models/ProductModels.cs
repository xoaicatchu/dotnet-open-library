namespace SwaggerDocumentation.Api.Models;

/// <summary>
/// Model đại diện cho sản phẩm phiên bản V1.
/// </summary>
public record ProductV1(
    int Id,
    string Name,
    decimal Price,
    string Category
);

/// <summary>
/// Yêu cầu tạo sản phẩm phiên bản V1.
/// </summary>
public record CreateProductV1Request(
    string Name,
    decimal Price,
    string Category
);

/// <summary>
/// Model đại diện cho sản phẩm phiên bản V2 với nhiều thuộc tính nâng cao.
/// </summary>
public record ProductV2(
    int Id,
    string Sku,
    string Name,
    decimal Price,
    string Category,
    double Rating,
    bool InStock,
    List<string> Tags
);

/// <summary>
/// Yêu cầu tạo sản phẩm phiên bản V2.
/// </summary>
public record CreateProductV2Request(
    string Sku,
    string Name,
    decimal Price,
    string Category,
    bool InStock,
    List<string> Tags
);
