namespace CleanVerticalSlice.Application.Products.CreateProduct;

using System.Threading;
using System.Threading.Tasks;
using MediatR;
using CleanVerticalSlice.Application.Common.Interfaces;
using CleanVerticalSlice.Application.Products.GetProducts;
using CleanVerticalSlice.Domain.Products;

public class CreateProductHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IApplicationDbContext _context;

    public CreateProductHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var entity = Product.Create(
            request.Request.Name,
            request.Request.Description,
            request.Request.Price,
            request.Request.Stock,
            request.Request.CategoryId);

        _context.Products.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return new ProductDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Price = entity.Price.Amount
        };
    }
}
