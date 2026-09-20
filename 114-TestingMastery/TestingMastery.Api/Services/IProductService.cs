namespace TestingMastery.Api.Services;

public record ProductDto(int Id, string Name, decimal Price, int Stock);
public record CreateProductRequest(string Name, decimal Price, int Stock);
public record UpdateProductRequest(string Name, decimal Price, int Stock);

public interface IProductService
{
    Task<List<ProductDto>> GetAllAsync();
    Task<ProductDto?> GetByIdAsync(int id);
    Task<ProductDto> CreateAsync(CreateProductRequest req);
    Task<ProductDto?> UpdateAsync(int id, UpdateProductRequest req);
    Task<bool> DeleteAsync(int id);
}
