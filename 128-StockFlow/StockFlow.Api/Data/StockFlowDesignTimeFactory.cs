using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace StockFlow.Api.Data;
public sealed class StockFlowDesignTimeFactory : IDesignTimeDbContextFactory<StockFlowDbContext>
{
    public StockFlowDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<StockFlowDbContext>()
        .UseNpgsql(Environment.GetEnvironmentVariable("ConnectionStrings__StockFlow") ?? "Host=localhost;Database=stockflow;Username=stockflow").Options);
}
