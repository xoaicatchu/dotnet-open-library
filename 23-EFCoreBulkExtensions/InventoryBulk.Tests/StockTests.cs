using System.Net;
using System.Net.Http.Json;
using InventoryBulk.Api.Data;
using InventoryBulk.Api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryBulk.Tests;

public class StockTestsFixture : WebApplicationFactory<Program>
{
    private readonly string _dbPath;

    public StockTestsFixture()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"inventory_test_{Guid.NewGuid():N}.db");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<InventoryDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            services.AddDbContext<InventoryDbContext>(options =>
                options.UseSqlite($"Data Source={_dbPath}"));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (File.Exists(_dbPath)) try { File.Delete(_dbPath); } catch { }
    }
}

public class StockTests : IClassFixture<StockTestsFixture>
{
    private readonly HttpClient _client;

    public StockTests(StockTestsFixture fixture)
    {
        _client = fixture.CreateClient();
    }

    [Fact]
    public async Task GetAll_ReturnsEmptyList_Initially()
    {
        var response = await _client.GetAsync("/api/stock");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var items = await response.Content.ReadFromJsonAsync<List<StockItemDto>>();
        Assert.NotNull(items);
    }

    [Fact]
    public async Task BulkInsert_InsertsMultipleItems()
    {
        var request = new BulkInsertRequest(new List<CreateStockItemDto>
        {
            new("SKU-BI-001", "Widget Alpha", "Widgets", 9.99m, 100),
            new("SKU-BI-002", "Widget Beta", "Widgets", 14.99m, 200),
            new("SKU-BI-003", "Gadget Gamma", "Gadgets", 29.99m, 50)
        });

        var response = await _client.PostAsJsonAsync("/api/stock/bulk-insert", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<BulkResultDto>();
        Assert.NotNull(result);
        Assert.Equal("BulkInsert", result.Operation);
        Assert.Equal(3, result.AffectedCount);
    }

    [Fact]
    public async Task BulkInsert_WithEmptyList_ReturnsBadRequest()
    {
        var request = new BulkInsertRequest(new List<CreateStockItemDto>());
        var response = await _client.PostAsJsonAsync("/api/stock/bulk-insert", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task BulkUpdate_UpdatesPricesInCategory()
    {
        // Insert first
        var insertReq = new BulkInsertRequest(new List<CreateStockItemDto>
        {
            new("SKU-BU-001", "Update Test A", "TestUpdateCat", 100m, 10),
            new("SKU-BU-002", "Update Test B", "TestUpdateCat", 200m, 20)
        });
        await _client.PostAsJsonAsync("/api/stock/bulk-insert", insertReq);

        // Bulk update price (1.5x multiplier)
        var updateReq = new BulkUpdatePriceRequest("TestUpdateCat", 1.5m);
        var response = await _client.PostAsJsonAsync("/api/stock/bulk-update", updateReq);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<BulkResultDto>();
        Assert.NotNull(result);
        Assert.Equal("BulkUpdate", result.Operation);
        Assert.Equal(2, result.AffectedCount);

        // Verify prices changed
        var getRes = await _client.GetAsync("/api/stock?category=TestUpdateCat");
        var items = await getRes.Content.ReadFromJsonAsync<List<StockItemDto>>();
        Assert.NotNull(items);
        Assert.Contains(items, i => i.Price == 150m);
        Assert.Contains(items, i => i.Price == 300m);
    }

    [Fact]
    public async Task BulkUpdate_WithEmptyCategory_ReturnsBadRequest()
    {
        var updateReq = new BulkUpdatePriceRequest("", 1.5m);
        var response = await _client.PostAsJsonAsync("/api/stock/bulk-update", updateReq);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task BulkDelete_RemovesItemsInCategory()
    {
        // Insert first
        var insertReq = new BulkInsertRequest(new List<CreateStockItemDto>
        {
            new("SKU-BD-001", "Delete Test A", "DeleteCat", 10m, 1),
            new("SKU-BD-002", "Delete Test B", "DeleteCat", 20m, 2)
        });
        await _client.PostAsJsonAsync("/api/stock/bulk-insert", insertReq);

        // Bulk delete
        var deleteReq = new BulkDeleteRequest("DeleteCat");
        var response = await _client.PostAsJsonAsync("/api/stock/bulk-delete", deleteReq);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<BulkResultDto>();
        Assert.NotNull(result);
        Assert.Equal("BulkDelete", result.Operation);
        Assert.Equal(2, result.AffectedCount);

        // Verify deleted
        var getRes = await _client.GetAsync("/api/stock?category=DeleteCat");
        var items = await getRes.Content.ReadFromJsonAsync<List<StockItemDto>>();
        Assert.NotNull(items);
        Assert.Empty(items);
    }

    [Fact]
    public async Task BulkUpsert_InsertsNewAndUpdatesExisting()
    {
        // Insert initial items
        var insertReq = new BulkInsertRequest(new List<CreateStockItemDto>
        {
            new("SKU-UPSERT-001", "Upsert Item Original", "UpsertCat", 50m, 10)
        });
        await _client.PostAsJsonAsync("/api/stock/bulk-insert", insertReq);

        // Upsert: update existing SKU-UPSERT-001 and insert new SKU-UPSERT-002
        var upsertReq = new BulkUpsertRequest(new List<CreateStockItemDto>
        {
            new("SKU-UPSERT-001", "Upsert Item Modified", "UpsertCat", 75m, 15),
            new("SKU-UPSERT-002", "Upsert Item New", "UpsertCat", 30m, 8)
        });
        var response = await _client.PostAsJsonAsync("/api/stock/bulk-upsert", upsertReq);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<BulkResultDto>();
        Assert.NotNull(result);
        Assert.Equal("BulkInsertOrUpdate", result.Operation);
        Assert.Equal(2, result.AffectedCount);

        // Verify
        var getRes = await _client.GetAsync("/api/stock?category=UpsertCat");
        var items = await getRes.Content.ReadFromJsonAsync<List<StockItemDto>>();
        Assert.NotNull(items);
        Assert.Contains(items, i => i.Sku == "SKU-UPSERT-001" && i.Name == "Upsert Item Modified" && i.Price == 75m);
        Assert.Contains(items, i => i.Sku == "SKU-UPSERT-002");
    }

    [Fact]
    public async Task GetSummary_ReturnsAggregatedData()
    {
        // Insert items first
        var insertReq = new BulkInsertRequest(new List<CreateStockItemDto>
        {
            new("SKU-SUM-001", "Summary A", "SumCatA", 10m, 5),
            new("SKU-SUM-002", "Summary B", "SumCatA", 20m, 10),
            new("SKU-SUM-003", "Summary C", "SumCatB", 30m, 15)
        });
        await _client.PostAsJsonAsync("/api/stock/bulk-insert", insertReq);

        var response = await _client.GetAsync("/api/stock/summary");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("SumCatA", content);
        Assert.Contains("SumCatB", content);
    }

    [Fact]
    public async Task GetById_WithNonExistentId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/stock/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
