using RepoDb;
using WarehouseManagement.Api.Entities;

namespace WarehouseManagement.Api.Data;

public static class DbInitializer
{
    static DbInitializer()
    {
        GlobalConfiguration.Setup().UseSqlite();
    }

    public static void Initialize(IDbConnectionFactory factory)
    {
        GlobalConfiguration.Setup().UseSqlite();
        using var connection = factory.CreateConnection();

        // Create table using ExecuteNonQuery
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS WarehouseItems (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Sku TEXT NOT NULL UNIQUE,
                Name TEXT NOT NULL,
                Location TEXT NOT NULL,
                Quantity INTEGER NOT NULL,
                UnitCost DECIMAL(18,2) NOT NULL,
                LastRestockedAt DATETIME NOT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_WarehouseItems_Location ON WarehouseItems(Location);
        ";
        cmd.ExecuteNonQuery();

        // Check if empty using RepoDb CountAll
        var count = connection.CountAll<WarehouseItem>();
        if (count == 0)
        {
            var initialItems = new List<WarehouseItem>
            {
                new() { Sku = "SKU-PALLET-001", Name = "Standard Wooden Pallet", Location = "Aisle-A1", Quantity = 250, UnitCost = 15.50m, LastRestockedAt = DateTime.UtcNow.AddDays(-10) },
                new() { Sku = "SKU-BOX-M", Name = "Medium Shipping Box 12x12x12", Location = "Aisle-A2", Quantity = 1200, UnitCost = 1.25m, LastRestockedAt = DateTime.UtcNow.AddDays(-5) },
                new() { Sku = "SKU-TAPE-HD", Name = "Heavy Duty Packing Tape", Location = "Aisle-B1", Quantity = 450, UnitCost = 3.99m, LastRestockedAt = DateTime.UtcNow.AddDays(-2) },
                new() { Sku = "SKU-WRAP-500", Name = "Stretch Wrap Roll 500m", Location = "Aisle-B2", Quantity = 80, UnitCost = 22.00m, LastRestockedAt = DateTime.UtcNow.AddDays(-1) }
            };

            // RepoDb Batch Operation: InsertAll
            connection.InsertAll(initialItems);
        }
    }
}
