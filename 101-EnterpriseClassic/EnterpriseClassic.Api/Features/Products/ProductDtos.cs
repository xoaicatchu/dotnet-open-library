using AutoMapper;

namespace EnterpriseClassic.Api.Features.Products;

public record ProductSummaryDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public double Price { get; init; }
    public int CategoryId { get; init; }
}
public record ProductDto(int Id, string Name, string Description, decimal Price, int Stock, int CategoryId, DateTime CreatedAt);
public record CreateProductRequest(string Name, string Description, decimal Price, int Stock, int CategoryId);
public record UpdateProductRequest(string Name, string Description, decimal Price, int Stock, int CategoryId);

public class ProductProfile : Profile
{
    public ProductProfile()
    {
        CreateMap<ProductEntity, ProductSummaryDto>();
        CreateMap<ProductEntity, ProductDto>();
        CreateMap<CreateProductRequest, ProductEntity>();
    }
}
