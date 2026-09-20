using System.Data;
using Dapper;
using ProductCatalog.Api.Data;
using ProductCatalog.Api.Entities;

namespace ProductCatalog.Api.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly IDbConnectionFactory _factory;

    public ProductRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<IEnumerable<Product>> GetAllAsync(string? search = null, int? categoryId = null)
    {
        using var connection = _factory.CreateConnection();

        var sql = @"
            SELECT 
                p.Id, p.Name, p.Description, p.Price, p.Stock, p.CategoryId,
                c.Id AS CategoryId, c.Name, c.Description
            FROM Products p
            LEFT JOIN Categories c ON p.CategoryId = c.Id
            WHERE (@Search IS NULL OR p.Name LIKE @SearchPattern OR p.Description LIKE @SearchPattern)
              AND (@CategoryId IS NULL OR p.CategoryId = @CategoryId)
            ORDER BY p.Id ASC;";

        var parameters = new
        {
            Search = search,
            SearchPattern = string.IsNullOrWhiteSpace(search) ? null : $"%{search}%",
            CategoryId = categoryId
        };

        var products = await connection.QueryAsync<Product, Category, Product>(
            sql,
            (product, category) =>
            {
                product.Category = category;
                return product;
            },
            param: parameters,
            splitOn: "CategoryId"
        );

        return products;
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        using var connection = _factory.CreateConnection();

        var sql = @"
            SELECT 
                p.Id, p.Name, p.Description, p.Price, p.Stock, p.CategoryId,
                c.Id AS CategoryId, c.Name, c.Description
            FROM Products p
            LEFT JOIN Categories c ON p.CategoryId = c.Id
            WHERE p.Id = @Id;";

        var products = await connection.QueryAsync<Product, Category, Product>(
            sql,
            (product, category) =>
            {
                product.Category = category;
                return product;
            },
            param: new { Id = id },
            splitOn: "CategoryId"
        );

        return products.FirstOrDefault();
    }

    public async Task<int> CreateAsync(Product product)
    {
        using var connection = _factory.CreateConnection();

        var sql = @"
            INSERT INTO Products (Name, Description, Price, Stock, CategoryId)
            VALUES (@Name, @Description, @Price, @Stock, @CategoryId);
            SELECT last_insert_rowid();";

        return await connection.ExecuteScalarAsync<int>(sql, product);
    }

    public async Task<bool> UpdateAsync(Product product)
    {
        using var connection = _factory.CreateConnection();

        var sql = @"
            UPDATE Products
            SET Name = @Name,
                Description = @Description,
                Price = @Price,
                Stock = @Stock,
                CategoryId = @CategoryId
            WHERE Id = @Id;";

        var rowsAffected = await connection.ExecuteAsync(sql, product);
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = _factory.CreateConnection();

        var sql = "DELETE FROM Products WHERE Id = @Id;";
        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
        return rowsAffected > 0;
    }

    public async Task<int> ApplyBatchDiscountAsync(decimal discountPercentage, IEnumerable<int> productIds)
    {
        using var connection = _factory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            var rate = discountPercentage / 100m;
            var sql = @"
                UPDATE Products
                SET Price = ROUND(Price * (1.0 - @Rate), 2)
                WHERE Id = @Id;";

            var count = 0;
            foreach (var id in productIds)
            {
                var affected = await connection.ExecuteAsync(sql, new { Rate = rate, Id = id }, transaction);
                count += affected;
            }

            transaction.Commit();
            return count;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> CategoryExistsAsync(int categoryId)
    {
        using var connection = _factory.CreateConnection();
        var count = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM Categories WHERE Id = @CategoryId;",
            new { CategoryId = categoryId });
        return count > 0;
    }
}
