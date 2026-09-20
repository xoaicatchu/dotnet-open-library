using Dapper;

namespace ProductCatalog.Api.Data;

public static class DbInitializer
{
    static DbInitializer()
    {
        SqlMapper.AddTypeHandler(new SqliteDecimalHandler());
    }

    public static void Initialize(IDbConnectionFactory factory)
    {
        SqlMapper.AddTypeHandler(new SqliteDecimalHandler());
        using var connection = factory.CreateConnection();

        // Create Categories table
        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Categories (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Description TEXT
            );
        ");

        // Create Products table
        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Products (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Description TEXT NOT NULL,
                Price DECIMAL(18,2) NOT NULL,
                Stock INTEGER NOT NULL,
                CategoryId INTEGER NOT NULL,
                FOREIGN KEY (CategoryId) REFERENCES Categories(Id) ON DELETE RESTRICT
            );
            CREATE INDEX IF NOT EXISTS IX_Products_CategoryId ON Products(CategoryId);
            CREATE INDEX IF NOT EXISTS IX_Products_Name ON Products(Name);
        ");

        // Seed sample data if empty
        var categoryCount = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Categories;");
        if (categoryCount == 0)
        {
            connection.Execute(@"
                INSERT INTO Categories (Name, Description) VALUES
                ('Electronics', 'Gadgets and electronic devices'),
                ('Books', 'Physical and electronic books'),
                ('Home & Kitchen', 'Household items and appliances');
            ");

            connection.Execute(@"
                INSERT INTO Products (Name, Description, Price, Stock, CategoryId) VALUES
                ('Wireless Mouse', 'Ergonomic 2.4GHz wireless mouse', 29.99, 150, 1),
                ('Mechanical Keyboard', 'RGB mechanical gaming keyboard', 89.99, 80, 1),
                ('Clean Architecture Book', 'A Craftsman Guide to Software Structure and Design', 34.50, 45, 2),
                ('Stainless Steel Kettle', '1.7L fast boiling electric kettle', 45.00, 60, 3);
            ");
        }
    }
}
