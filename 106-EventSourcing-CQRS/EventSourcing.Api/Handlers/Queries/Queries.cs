using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventSourcing.Api.ReadModel;
namespace EventSourcing.Api.Handlers.Queries;

public record GetProductsQuery();
public record GetProductByIdQuery(Guid Id);

public class GetProductsHandler
{
    private readonly IProductReadRepository _readRepo;
    public GetProductsHandler(IProductReadRepository readRepo) => _readRepo = readRepo;
    public Task<List<ProductReadModel>> Handle(GetProductsQuery query) => _readRepo.GetAllAsync();
}

public class GetProductByIdHandler
{
    private readonly IProductReadRepository _readRepo;
    public GetProductByIdHandler(IProductReadRepository readRepo) => _readRepo = readRepo;
    public Task<ProductReadModel?> Handle(GetProductByIdQuery query) => _readRepo.GetByIdAsync(query.Id);
}
