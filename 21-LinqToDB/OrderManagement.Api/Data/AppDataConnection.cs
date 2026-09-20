using LinqToDB;
using LinqToDB.Configuration;
using LinqToDB.Data;
using OrderManagement.Api.Entities;

namespace OrderManagement.Api.Data;

public class AppDataConnection : DataConnection
{
    public AppDataConnection(DataOptions<AppDataConnection> options)
        : base(options.Options)
    {
    }

    public ITable<Order> Orders => this.GetTable<Order>();
    public ITable<OrderItem> OrderItems => this.GetTable<OrderItem>();
}
