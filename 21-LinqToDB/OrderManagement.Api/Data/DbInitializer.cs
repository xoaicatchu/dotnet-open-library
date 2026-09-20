using LinqToDB;
using LinqToDB.Data;
using OrderManagement.Api.Entities;

namespace OrderManagement.Api.Data;

public static class DbInitializer
{
    public static void Initialize(AppDataConnection db)
    {
        db.CreateTable<Order>(tableOptions: TableOptions.CreateIfNotExists);
        db.CreateTable<OrderItem>(tableOptions: TableOptions.CreateIfNotExists);

        if (!db.Orders.Any())
        {
            var order1 = new Order
            {
                CustomerName = "Alice Nguyen",
                ShippingAddress = "123 Le Loi, District 1, HCMC",
                Status = "Processing",
                TotalAmount = 150.00m,
                CreatedAt = DateTime.UtcNow.AddHours(-5)
            };
            var order1Id = db.InsertWithInt32Identity(order1);

            var order2 = new Order
            {
                CustomerName = "Bob Tran",
                ShippingAddress = "456 Tran Hung Dao, Da Nang",
                Status = "Shipped",
                TotalAmount = 280.50m,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };
            var order2Id = db.InsertWithInt32Identity(order2);

            var items = new List<OrderItem>
            {
                new() { OrderId = order1Id, ProductName = "Mechanical Keyboard", Quantity = 1, UnitPrice = 100.00m },
                new() { OrderId = order1Id, ProductName = "Mousepad XL", Quantity = 2, UnitPrice = 25.00m },
                new() { OrderId = order2Id, ProductName = "Gaming Monitor 27\"", Quantity = 1, UnitPrice = 280.50m }
            };

            // LinqToDB BulkCopy demo
            db.BulkCopy(items);
        }
    }
}
