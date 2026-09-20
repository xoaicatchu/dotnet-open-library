using System.Text.Json;
using AutoMapper;
using EnterpriseClassic.Api.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace EnterpriseClassic.Api.Features.Products;

public record GetProductByIdQuery(int Id) : IRequest<ProductDto?>;

public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDto?>
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;
    private readonly IDistributedCache _cache;

    public GetProductByIdQueryHandler(AppDbContext db, IMapper mapper, IDistributedCache cache)
    {
        _db = db;
        _mapper = mapper;
        _cache = cache;
    }

    public async Task<ProductDto?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"Product_{request.Id}";
        var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrEmpty(cached))
        {
            return JsonSerializer.Deserialize<ProductDto>(cached);
        }

        var product = await _db.Products.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (product == null) return null;

        var dto = _mapper.Map<ProductDto>(product);
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dto), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        }, cancellationToken);

        return dto;
    }
}
