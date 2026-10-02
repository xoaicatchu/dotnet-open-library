using Microsoft.EntityFrameworkCore;
using MultiTenant.Api.Core.Entities;
using MultiTenant.Api.Infrastructure.Context;

namespace MultiTenant.Tests;

public class DataIsolationTests
{
    private ApplicationDbContext CreateDbContext(string tenantId, DbContextOptions<ApplicationDbContext> options)
    {
        var tenantContext = new TenantContext();
        tenantContext.SetTenant(new Tenant
        {
            Id = tenantId,
            Name = $"Company {tenantId}",
            Subdomain = tenantId,
            IsActive = true
        });

        return new ApplicationDbContext(options, tenantContext);
    }

    [Fact]
    public async Task GlobalQueryFilter_ShouldOnlyReturn_Products_BelongingToCurrentTenant()
    {
        // Arrange: Dùng chung 1 SQLite in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Thêm sản phẩm của Tenant A
        using (var dbA = CreateDbContext("tenant-a", options))
        {
            dbA.Products.Add(new Product { Name = "Laptop Dell", Sku = "DELL-01", Price = 1200 });
            dbA.Products.Add(new Product { Name = "Mouse Logitech", Sku = "LOGI-01", Price = 50 });
            await dbA.SaveChangesAsync();
        }

        // Thêm sản phẩm của Tenant B
        using (var dbB = CreateDbContext("tenant-b", options))
        {
            dbB.Products.Add(new Product { Name = "MacBook Pro", Sku = "APPLE-01", Price = 2500 });
            await dbB.SaveChangesAsync();
        }

        // Act & Assert cho Tenant A: Chỉ thấy 2 sản phẩm của mình
        using (var queryDbA = CreateDbContext("tenant-a", options))
        {
            var productsA = await queryDbA.Products.ToListAsync();
            Assert.Equal(2, productsA.Count);
            Assert.All(productsA, p => Assert.Equal("tenant-a", p.TenantId));
        }

        // Act & Assert cho Tenant B: Chỉ thấy 1 sản phẩm của mình
        using (var queryDbB = CreateDbContext("tenant-b", options))
        {
            var productsB = await queryDbB.Products.ToListAsync();
            Assert.Single(productsB);
            Assert.Equal("MacBook Pro", productsB[0].Name);
            Assert.Equal("tenant-b", productsB[0].TenantId);
        }
    }

    [Fact]
    public async Task SaveChanges_ShouldAutomaticallyAssign_TenantId_WhenEmpty()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var db = CreateDbContext("tenant-auto", options);
        var product = new Product
        {
            Name = "Monitor LG",
            Sku = "LG-27",
            Price = 300
            // TenantId cố tình để trống
        };

        // Act
        db.Products.Add(product);
        await db.SaveChangesAsync();

        // Assert
        Assert.Equal("tenant-auto", product.TenantId);
    }

    [Fact]
    public async Task SaveChanges_ShouldThrow_WhenTenant_TriesToInject_AnotherTenantId()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var db = CreateDbContext("tenant-attacker", options);
        var product = new Product
        {
            Name = "Hacked Product",
            Sku = "HACK-01",
            Price = 10,
            TenantId = "tenant-victim" // Cố tình inject TenantId khác
        };

        db.Products.Add(product);

        // Act & Assert: Phải bắn UnauthorizedAccessException ngăn chặn tấn công giả mạo tenant
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveChanges_ShouldThrow_WhenTenantId_IsModified()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        Product product;
        using (var db = CreateDbContext("tenant-x", options))
        {
            product = new Product { Name = "Keyboard", Sku = "KB-01", Price = 80 };
            db.Products.Add(product);
            await db.SaveChangesAsync();
        }

        // Act & Assert: Thử đổi quyền sở hữu sang Tenant Y
        using (var updateDb = CreateDbContext("tenant-x", options))
        {
            var existing = await updateDb.Products.FirstAsync(p => p.Id == product.Id);
            existing.TenantId = "tenant-y"; // Thử đổi TenantId

            await Assert.ThrowsAsync<InvalidOperationException>(() => updateDb.SaveChangesAsync());
        }
    }

    [Fact]
    public async Task IgnoreQueryFilters_ShouldAllow_SuperAdmin_ToSeeAllTenantsData()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using (var dbA = CreateDbContext("tenant-1", options))
        {
            dbA.Products.Add(new Product { Name = "P1", Sku = "S1", Price = 10 });
            await dbA.SaveChangesAsync();
        }

        using (var dbB = CreateDbContext("tenant-2", options))
        {
            dbB.Products.Add(new Product { Name = "P2", Sku = "S2", Price = 20 });
            await dbB.SaveChangesAsync();
        }

        // Act: Dùng IgnoreQueryFilters (tương đương kịch bản System Admin / Báo cáo tổng thể)
        using (var adminDb = CreateDbContext("tenant-1", options))
        {
            var allProducts = await adminDb.Products.IgnoreQueryFilters().ToListAsync();

            // Assert: Thấy toàn bộ dữ liệu của cả 2 tenant
            Assert.Equal(2, allProducts.Count);
        }
    }
}
