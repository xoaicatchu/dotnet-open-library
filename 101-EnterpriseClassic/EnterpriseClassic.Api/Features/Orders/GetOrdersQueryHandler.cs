using Dapper;
using EnterpriseClassic.Api.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseClassic.Api.Features.Orders;

public record GetOrdersQuery : IRequest<List<OrderSummaryDto>>;

public class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, List<OrderSummaryDto>>
{
    private readonly AppDbContext _db;
    public GetOrdersQueryHandler(AppDbContext db) => _db = db;

    public async Task<List<OrderSummaryDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var sql = "SELECT Id, CustomerName, CAST(TotalAmount AS REAL) as TotalAmount, Status FROM Orders";
        var connection = _db.Database.GetDbConnection();
        var result = await connection.QueryAsync<OrderSummaryDto>(sql);
        return result.ToList();
    }
}
