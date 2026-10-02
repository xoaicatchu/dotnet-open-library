using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StockFlow.Api.Data;
using StockFlow.Tests.Infrastructure;

namespace StockFlow.Tests;

[Collection("Postgres")]
public sealed class ReceiptTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Draft_does_not_change_balance_post_is_idempotent_and_posted_document_is_immutable()
    {
        await using var factory = new StockFlowApiFactory(postgres.Database.GetConnectionString());
        var client = await LoginAsync(factory, "Warehouse");
        var admin = await LoginAsync(factory, "Admin");
        var supplierId = await CreateSupplierAsync(admin);
        var productId = await CreateProductAsync(admin);
        var documentId = Guid.NewGuid();
        var draft = await CreateReceiptAsync(client, documentId, supplierId, (productId, 100L, 250L));
        Assert.Equal(HttpStatusCode.Created, draft.StatusCode);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<StockFlowDbContext>();
            Assert.Equal(0, await db.StockBalances.Where(x => x.ProductId == productId).Select(x => x.Quantity).SingleAsync());
            Assert.Empty(await db.StockMovements.Where(x => x.DocumentId == documentId).ToListAsync());
        }

        var post = await client.PostAsJsonAsync($"/api/documents/{documentId}/post", new { expectedVersion = 1L });
        Assert.Equal(HttpStatusCode.OK, post.StatusCode);
        var result = await post.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Posted", result.GetProperty("status").GetString());
        Assert.Equal(25_000L, result.GetProperty("totalCostVnd").GetInt64());

        var repeated = await client.PostAsJsonAsync($"/api/documents/{documentId}/post", new { expectedVersion = 1L });
        Assert.Equal(HttpStatusCode.OK, repeated.StatusCode);
        Assert.Equal(result.GetProperty("version").GetInt64(), (await repeated.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("version").GetInt64());

        var edit = await client.PutAsJsonAsync($"/api/documents/{documentId}", new
        {
            expectedVersion = 2L,
            supplierId,
            lines = new[] { new { productId, quantity = 99L, unitCostVnd = 250L } }
        });
        Assert.Equal(HttpStatusCode.Conflict, edit.StatusCode);

        await using var verify = factory.Services.CreateAsyncScope();
        var database = verify.ServiceProvider.GetRequiredService<StockFlowDbContext>();
        Assert.Equal(100L, await database.StockBalances.Where(x => x.ProductId == productId).Select(x => x.Quantity).SingleAsync());
        Assert.Single(await database.StockMovements.Where(x => x.DocumentId == documentId).ToListAsync());
        var unitChange = await admin.PutAsJsonAsync($"/api/products/{productId}", new
        {
            sku = $"UPDATED-{Guid.NewGuid():N}",
            name = "Sản phẩm",
            unit = "thùng",
            purchasePriceVnd = 100L,
            salePriceVnd = 150L,
            minimumStock = 0L,
            isActive = true
        });
        Assert.Equal(HttpStatusCode.Conflict, unitChange.StatusCode);
    }

    [Fact]
    public async Task Failed_second_line_rolls_back_first_balance_and_movement()
    {
        await using var factory = new StockFlowApiFactory(postgres.Database.GetConnectionString());
        var client = await LoginAsync(factory, "Warehouse");
        var admin = await LoginAsync(factory, "Admin");
        var supplierId = await CreateSupplierAsync(admin);
        var productIds = new[] { await CreateProductAsync(admin), await CreateProductAsync(admin) }.Order().ToArray();
        var firstProduct = productIds[0];
        var secondProduct = productIds[1];
        var documentId = Guid.NewGuid();
        var create = await CreateReceiptAsync(client, documentId, supplierId, (firstProduct, 1L, 1L), (secondProduct, 1L, 1L));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        // Force the second balance version to the safe-integer ceiling so its guarded update
        // fails after the first balance update has already run inside the transaction.
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<StockFlowDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"StockBalances\" SET \"Version\" = 9007199254740991 WHERE \"ProductId\" = {secondProduct}");
        }

        var posted = await client.PostAsJsonAsync($"/api/documents/{documentId}/post", new { expectedVersion = 1L });
        Assert.Equal(HttpStatusCode.BadRequest, posted.StatusCode);

        await using var verify = factory.Services.CreateAsyncScope();
        var database = verify.ServiceProvider.GetRequiredService<StockFlowDbContext>();
        Assert.Equal(0L, await database.StockBalances.Where(x => x.ProductId == firstProduct).Select(x => x.Quantity).SingleAsync());
        Assert.Equal(0L, await database.StockBalances.Where(x => x.ProductId == secondProduct).Select(x => x.Quantity).SingleAsync());
        Assert.Empty(await database.StockMovements.Where(x => x.DocumentId == documentId).ToListAsync());
        Assert.Equal(DocumentStatus.Draft, await database.StockDocuments.Where(x => x.Id == documentId).Select(x => x.Status).SingleAsync());
    }

    [Fact]
    public async Task Receipt_rejects_duplicate_lines_zero_quantity_and_unsafe_total()
    {
        await using var factory = new StockFlowApiFactory(postgres.Database.GetConnectionString());
        var client = await LoginAsync(factory, "Warehouse");
        var admin = await LoginAsync(factory, "Admin");
        var supplierId = await CreateSupplierAsync(admin);
        var productId = await CreateProductAsync(admin);
        var duplicateId = Guid.NewGuid();
        var duplicate = await CreateReceiptAsync(client, duplicateId, supplierId, (productId, 1L, 1L), (productId, 2L, 1L));
        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);

        var zero = await CreateReceiptAsync(client, Guid.NewGuid(), supplierId, (productId, 0L, 1L));
        Assert.Equal(HttpStatusCode.BadRequest, zero.StatusCode);

        var overflow = await CreateReceiptAsync(client, Guid.NewGuid(), supplierId, (productId, 9_007_199_254_740_991L, 2L));
        Assert.Equal(HttpStatusCode.BadRequest, overflow.StatusCode);
    }

    [Fact]
    public async Task Posted_multiline_receipt_writes_one_movement_per_line_and_reconciles_balance()
    {
        await using var factory = new StockFlowApiFactory(postgres.Database.GetConnectionString());
        var warehouse = await LoginAsync(factory, "Warehouse");
        var admin = await LoginAsync(factory, "Admin");
        var supplierId = await CreateSupplierAsync(admin);
        var productIds = new[] { await CreateProductAsync(admin), await CreateProductAsync(admin) };
        var documentId = Guid.NewGuid();
        var draft = await CreateReceiptAsync(warehouse, documentId, supplierId,
            (productIds[0], 3L, 25L), (productIds[1], 7L, 40L));
        Assert.Equal(HttpStatusCode.Created, draft.StatusCode);
        var posted = await warehouse.PostAsJsonAsync($"/api/documents/{documentId}/post", new { expectedVersion = 1L });
        Assert.Equal(HttpStatusCode.OK, posted.StatusCode);
        var response = await posted.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(355L, response.GetProperty("totalCostVnd").GetInt64());
        Assert.True(response.GetProperty("postedAtUtc").TryGetDateTime(out var postedAt));
        Assert.Equal(DateTimeKind.Utc, postedAt.Kind);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StockFlowDbContext>();
        foreach (var productId in productIds)
        {
            var balance = await db.StockBalances.Where(item => item.ProductId == productId).Select(item => item.Quantity).SingleAsync();
            var movements = await db.StockMovements.Where(item => item.ProductId == productId).ToListAsync();
            Assert.Single(movements);
            Assert.Equal(balance, movements.Sum(movement => movement.Delta));
        }
    }

    [Fact]
    public async Task Product_unit_cannot_change_while_a_draft_references_it()
    {
        await using var factory = new StockFlowApiFactory(postgres.Database.GetConnectionString());
        var warehouse = await LoginAsync(factory, "Warehouse");
        var admin = await LoginAsync(factory, "Admin");
        var supplierId = await CreateSupplierAsync(admin);
        var productId = await CreateProductAsync(admin);
        var draftId = Guid.NewGuid();
        Assert.Equal(HttpStatusCode.Created, (await CreateReceiptAsync(warehouse, draftId, supplierId, (productId, 2L, 10L))).StatusCode);

        var changed = await admin.PutAsJsonAsync($"/api/products/{productId}", new
        {
            sku = $"UPDATED-{Guid.NewGuid():N}", name = "Sản phẩm", unit = "thùng", purchasePriceVnd = 100L,
            salePriceVnd = 150L, minimumStock = 0L, isActive = true
        });
        Assert.Equal(HttpStatusCode.Conflict, changed.StatusCode);
        Assert.Equal((int)HttpStatusCode.Conflict, (await changed.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetInt32());
        var product = await admin.GetFromJsonAsync<JsonElement>($"/api/products/{productId}");
        Assert.Equal("cái", product.GetProperty("unit").GetString());
    }

    [Fact]
    public async Task Racing_unit_update_and_post_keep_the_draft_unit_and_commit_one_movement()
    {
        await using var factory = new StockFlowApiFactory(postgres.Database.GetConnectionString());
        var warehouse = await LoginAsync(factory, "Warehouse");
        var admin = await LoginAsync(factory, "Admin");
        var supplierId = await CreateSupplierAsync(admin);
        var productId = await CreateProductAsync(admin);
        var draftId = Guid.NewGuid();
        Assert.Equal(HttpStatusCode.Created, (await CreateReceiptAsync(warehouse, draftId, supplierId, (productId, 2L, 10L))).StatusCode);

        var unitChange = admin.PutAsJsonAsync($"/api/products/{productId}", new
        {
            sku = $"RACE-{Guid.NewGuid():N}", name = "Sản phẩm", unit = "thùng", purchasePriceVnd = 100L,
            salePriceVnd = 150L, minimumStock = 0L, isActive = true
        });
        var posting = warehouse.PostAsJsonAsync($"/api/documents/{draftId}/post", new { expectedVersion = 1L });
        await Task.WhenAll(unitChange, posting);

        Assert.Equal(HttpStatusCode.Conflict, unitChange.Result.StatusCode);
        Assert.Equal(HttpStatusCode.OK, posting.Result.StatusCode);
        var current = await admin.GetFromJsonAsync<JsonElement>($"/api/products/{productId}");
        Assert.Equal("cái", current.GetProperty("unit").GetString());
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StockFlowDbContext>();
        Assert.Equal(2L, await db.StockBalances.Where(item => item.ProductId == productId).Select(item => item.Quantity).SingleAsync());
        Assert.Single(await db.StockMovements.Where(item => item.DocumentId == draftId).ToListAsync());
    }

    private static async Task<Guid> CreateProductAsync(HttpClient client)
    {
        var result = await client.PostAsJsonAsync("/api/products", new
        {
            sku = $"P-{Guid.NewGuid():N}",
            name = "Sản phẩm",
            unit = "cái",
            purchasePriceVnd = 100L,
            salePriceVnd = 150L,
            minimumStock = 0L
        });
        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        return (await result.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreateSupplierAsync(HttpClient client)
    {
        var result = await client.PostAsJsonAsync("/api/suppliers", new { code = $"S-{Guid.NewGuid():N}", name = "Nhà cung cấp" });
        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        return (await result.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private static Task<HttpResponseMessage> CreateReceiptAsync(HttpClient client, Guid id, Guid supplierId, params (Guid ProductId, long Quantity, long UnitCostVnd)[] lines) =>
        client.PostAsJsonAsync("/api/documents", new
        {
            id,
            expectedVersion = 0L,
            type = "Receipt",
            supplierId,
            lines = lines.Select(x => new { productId = x.ProductId, quantity = x.Quantity, unitCostVnd = x.UnitCostVnd })
        });

    private static async Task<HttpClient> LoginAsync(StockFlowApiFactory factory, string role)
    {
        var email = $"{role.ToLowerInvariant()}-{Guid.NewGuid():N}@stockflow.test";
        var password = $"Valid!{Guid.NewGuid():N}";
        await factory.CreateUserAsync(email, password, role);
        var client = factory.CreateClient();
        await AuthTests.RefreshCsrfAsync(client);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/auth/login", new { email, password })).StatusCode);
        await AuthTests.RefreshCsrfAsync(client);
        return client;
    }
}
