namespace FakeDataGenerator.Api.Models;

public record OrderItem(
    Guid Id,
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal TotalPrice
);

public record CustomerOrder(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    string CustomerName,
    string CustomerEmail,
    DateTime OrderDate,
    string Status,
    List<OrderItem> Items,
    decimal TotalAmount,
    UserAddress ShippingAddress
);

public record SeedDatabaseRequest(
    int NumberOfUsers = 10,
    int NumberOfOrders = 20,
    int? Seed = null
);

public record SeedDatabaseResponse(
    int UsersCreated,
    int OrdersCreated,
    long ExecutionTimeMs,
    UserProfile? SampleUser,
    CustomerOrder? SampleOrder
);
