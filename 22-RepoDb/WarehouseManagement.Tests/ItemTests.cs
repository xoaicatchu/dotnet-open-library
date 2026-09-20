using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using WarehouseManagement.Api.Data;
using WarehouseManagement.Api.Models;

namespace WarehouseManagement.Tests;

public class ItemTestsFixture : WebApplicationFactory<Program>
{
    private readonly string _dbPath;

    public ItemTestsFixture()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"warehouse_test_{Guid.NewGuid():N}.db");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IDbConnectionFactory));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            var connStr = $"Data Source={_dbPath}";
            var factory = new SqliteConnectionFactory(connStr);
            services.AddSingleton<IDbConnectionFactory>(factory);

            DbInitializer.Initialize(factory);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (File.Exists(_dbPath))
        {
            try { File.Delete(_dbPath); } catch { }
        }
    }
}

public class ItemTests : IClassFixture<ItemTestsFixture>
{
    private readonly HttpClient _client;

    public ItemTests(ItemTestsFixture fixture)
    {
        _client = fixture.CreateClient();
    }

    [Fact]
    public async Task GetAll_ReturnsSeededItems()
    {
        var response = await _client.GetAsync("/api/items");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var items = await response.Content.ReadFromJsonAsync<List<ItemResponseDto>>();
        Assert.NotNull(items);
        Assert.NotEmpty(items);
        Assert.All(items, i => Assert.True(i.TotalValue > 0));
    }

    [Fact]
    public async Task GetAll_WithLocationFilter_ReturnsMatchingItems()
    {
        var response = await _client.GetAsync("/api/items?location=Aisle-A1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var items = await response.Content.ReadFromJsonAsync<List<ItemResponseDto>>();
        Assert.NotNull(items);
        Assert.All(items, i => Assert.Equal("Aisle-A1", i.Location));
    }

    [Fact]
    public async Task GetAll_WithSearch_ReturnsMatchingItems()
    {
        var response = await _client.GetAsync("/api/items?search=Box");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var items = await response.Content.ReadFromJsonAsync<List<ItemResponseDto>>();
        Assert.NotNull(items);
        Assert.Contains(items, i => i.Name.Contains("Box"));
    }

    [Fact]
    public async Task GetById_WithValidId_ReturnsItem()
    {
        var response = await _client.GetAsync("/api/items/1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var item = await response.Content.ReadFromJsonAsync<ItemResponseDto>();
        Assert.NotNull(item);
        Assert.Equal(1, item.Id);
    }

    [Fact]
    public async Task GetById_WithNonExistentId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/items/9999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithValidData_ReturnsCreatedAndLocationHeader()
    {
        var createDto = new CreateItemDto(
            Sku: "SKU-TEST-NEW-01",
            Name: "Test Plastic Bin",
            Location: "Aisle-C2",
            Quantity: 60,
            UnitCost: 8.50m
        );

        var response = await _client.PostAsJsonAsync("/api/items", createDto);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var created = await response.Content.ReadFromJsonAsync<ItemResponseDto>();
        Assert.NotNull(created);
        Assert.Equal("SKU-TEST-NEW-01", created.Sku);
        Assert.Equal(510.0m, created.TotalValue);
    }

    [Fact]
    public async Task Create_WithExistingSku_ReturnsBadRequest()
    {
        var createDto = new CreateItemDto(
            Sku: "SKU-PALLET-001", // already seeded
            Name: "Duplicate Pallet",
            Location: "Aisle-Z",
            Quantity: 10,
            UnitCost: 10m
        );

        var response = await _client.PostAsJsonAsync("/api/items", createDto);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithValidData_ReturnsUpdatedItem()
    {
        var updateDto = new UpdateItemDto("Modified Tape", "Aisle-B1-Ext", 500, 4.25m);
        var response = await _client.PutAsJsonAsync("/api/items/3", updateDto);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await response.Content.ReadFromJsonAsync<ItemResponseDto>();
        Assert.NotNull(updated);
        Assert.Equal("Modified Tape", updated.Name);
        Assert.Equal(500, updated.Quantity);
    }

    [Fact]
    public async Task Update_WithNonExistentId_ReturnsNotFound()
    {
        var updateDto = new UpdateItemDto("Ghost", "Loc", 1, 1m);
        var response = await _client.PutAsJsonAsync("/api/items/8888", updateDto);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Upsert_WithExistingSku_UpdatesItem()
    {
        var upsertDto = new UpsertItemDto("SKU-WRAP-500", "Updated Stretch Wrap", "Aisle-B2", 95, 23.50m);
        var response = await _client.PostAsJsonAsync("/api/items/upsert", upsertDto);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var upserted = await response.Content.ReadFromJsonAsync<ItemResponseDto>();
        Assert.NotNull(upserted);
        Assert.Equal("Updated Stretch Wrap", upserted.Name);
        Assert.Equal(95, upserted.Quantity);
    }

    [Fact]
    public async Task Upsert_WithNewSku_CreatesItem()
    {
        var upsertDto = new UpsertItemDto("SKU-FORKLIFT-BAT", "Forklift Battery 48V", "Aisle-D1", 5, 1200.00m);
        var response = await _client.PostAsJsonAsync("/api/items/upsert", upsertDto);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var upserted = await response.Content.ReadFromJsonAsync<ItemResponseDto>();
        Assert.NotNull(upserted);
        Assert.Equal("SKU-FORKLIFT-BAT", upserted.Sku);
        Assert.Equal(5, upserted.Quantity);
    }

    [Fact]
    public async Task BatchRestock_UpdatesItemQuantitiesWithinTransaction()
    {
        var request = new BatchRestockRequest(
            Skus: new List<string> { "SKU-PALLET-001", "SKU-BOX-M" },
            AddedQuantity: 25
        );

        var response = await _client.PostAsJsonAsync("/api/items/batch-restock", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<BatchRestockResult>();
        Assert.NotNull(result);
        Assert.Equal(2, result.UpdatedCount);
    }

    [Fact]
    public async Task BatchRestock_WithInvalidQuantity_ReturnsBadRequest()
    {
        var request = new BatchRestockRequest(
            Skus: new List<string> { "SKU-PALLET-001" },
            AddedQuantity: -10
        );

        var response = await _client.PostAsJsonAsync("/api/items/batch-restock", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WithValidId_RemovesItem()
    {
        // First create item to delete
        var createDto = new CreateItemDto("SKU-DEL-TEMP", "Temp Item", "Aisle-X", 1, 1m);
        var createRes = await _client.PostAsJsonAsync("/api/items", createDto);
        var created = await createRes.Content.ReadFromJsonAsync<ItemResponseDto>();
        Assert.NotNull(created);

        var deleteRes = await _client.DeleteAsync($"/api/items/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteRes.StatusCode);

        var getRes = await _client.GetAsync($"/api/items/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getRes.StatusCode);
    }

    [Fact]
    public async Task Delete_WithNonExistentId_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync("/api/items/7777");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
