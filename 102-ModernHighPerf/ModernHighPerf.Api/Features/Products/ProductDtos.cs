namespace ModernHighPerf.Api.Features.Products;

public record ProductSummaryDto(int Id, string Name, decimal Price, int Stock);
public record ProductDetailDto(int Id, string Name, string Description, decimal Price, int Stock, int CategoryId, DateTime CreatedAt);

public record CreateProductRequest(string Name, string Description, decimal Price, int Stock, int CategoryId);
public record UpdateProductRequest(string Name, string Description, decimal Price, int Stock, int CategoryId);

public record GetProductsQuery;
public record GetProductByIdQuery(int Id);
public record CreateProductCommand(CreateProductRequest Request);
public record UpdateProductCommand(int Id, UpdateProductRequest Request);
