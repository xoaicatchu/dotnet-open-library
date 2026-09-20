namespace CleanVerticalSlice.Application.Products.UpdateProduct;

using System.Threading;
using System.Threading.Tasks;
using MediatR;
using CleanVerticalSlice.Application.Common.Interfaces;
using CleanVerticalSlice.Application.Products.GetProducts;

public class UpdateProductHandler : IRequestHandler<UpdateProductCommand, ProductDto?>
{
    private readonly IApplicationDbContext _context;

    public UpdateProductHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProductDto?> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Products.FindAsync(new object[] { request.Id }, cancellationToken);

        if (entity == null) return null;

        entity.Update(
            request.Request.Name,
            request.Request.Description,
            request.Request.Price,
            request.Request.Stock);

        await _context.SaveChangesAsync(cancellationToken);

        return new ProductDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Price = entity.Price.Amount
        };
    }
}
