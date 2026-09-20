using AutoMapper;
using EnterpriseClassic.Api.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace EnterpriseClassic.Api.Features.Products;

public record UpdateProductCommand(int Id, UpdateProductRequest Request) : IRequest<ProductDto?>;

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ProductDto?>
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;
    private readonly IDistributedCache _cache;

    public UpdateProductCommandHandler(AppDbContext db, IMapper mapper, IDistributedCache cache)
    {
        _db = db;
        _mapper = mapper;
        _cache = cache;
    }

    public async Task<ProductDto?> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _db.Products.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (product == null) return null;

        product.Name = request.Request.Name;
        product.Description = request.Request.Description;
        product.Price = request.Request.Price;
        product.Stock = request.Request.Stock;
        product.CategoryId = request.Request.CategoryId;

        await _db.SaveChangesAsync(cancellationToken);

        await _cache.RemoveAsync($"Product_{product.Id}", cancellationToken);

        return _mapper.Map<ProductDto>(product);
    }
}
