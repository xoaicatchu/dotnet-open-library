using Dapper;
using EnterpriseClassic.Api.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseClassic.Api.Features.Products;

public record GetProductsQuery : IRequest<List<ProductSummaryDto>>;

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, List<ProductSummaryDto>>
{
    private readonly AppDbContext _db;
    public GetProductsQueryHandler(AppDbContext db) => _db = db;

    public async Task<List<ProductSummaryDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var sql = "SELECT Id, Name, CAST(Price AS REAL) as Price, CategoryId FROM Products";
        var connection = _db.Database.GetDbConnection();
        var result = await connection.QueryAsync<ProductSummaryDto>(sql);
        return result.ToList();
    }
}
