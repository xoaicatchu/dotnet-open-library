using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StockFlow.Api.Data;
using StockFlow.Tests.Infrastructure;

namespace StockFlow.Tests;

[Collection("Postgres")]
public sealed class SyncIdempotencyTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Same_operation_and_fingerprint_replays_result_but_changed_payload_is_conflict()
    {
        await using var factory = new StockFlowApiFactory(postgres.Database.GetConnectionString());
        var client = await LoginAsync(factory);
        var admin = await LoginAdminAsync(factory);
        var supplierId = await CreateSupplierAsync(admin);
        var productId = await CreateProductAsync(admin);
        var clientOperationId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();

        var first = await SendAsync(client, clientOperationId, resourceId, supplierId, productId, 10L);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var firstOperation = (await first.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("results")[0];
        Assert.Equal("Accepted", firstOperation.GetProperty("status").GetString());
        Assert.Equal(resourceId, firstOperation.GetProperty("resourceId").GetGuid());

        var replay = await SendAsync(client, clientOperationId, resourceId, supplierId, productId, 10L);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Equal(firstOperation.GetProperty("result").GetRawText(),
            (await replay.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("results")[0].GetProperty("result").GetRawText());

        var changed = await SendAsync(client, clientOperationId, resourceId, supplierId, productId, 11L);
        Assert.Equal(HttpStatusCode.Conflict, changed.StatusCode);

        var updateOperationId = Guid.NewGuid();
        var update = await SendUpdateAsync(client, updateOperationId, resourceId, supplierId, productId, 12L);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updatedDocument = (await update.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("results")[0].GetProperty("result");
        Assert.Equal(2L, updatedDocument.GetProperty("version").GetInt64());
        Assert.Equal(120L, updatedDocument.GetProperty("totalCostVnd").GetInt64());
        var updateReplay = await SendUpdateAsync(client, updateOperationId, resourceId, supplierId, productId, 12L);
        Assert.Equal(HttpStatusCode.OK, updateReplay.StatusCode);
        Assert.Equal(2L, (await updateReplay.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("results")[0]
            .GetProperty("result").GetProperty("version").GetInt64());
        Assert.Equal(HttpStatusCode.Conflict, (await SendUpdateAsync(client, updateOperationId, resourceId, supplierId, productId, 13L)).StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StockFlowDbContext>();
        Assert.Equal(1, await db.StockDocuments.CountAsync(x => x.Id == resourceId));
        Assert.Equal(2, await db.ClientOperations.CountAsync(x => x.ClientOperationId == clientOperationId || x.ClientOperationId == updateOperationId));
        Assert.Equal(12L, await db.StockDocumentLines.Where(x => x.DocumentId == resourceId).Select(x => x.Quantity).SingleAsync());
        Assert.Equal(0L, await db.StockBalances.Where(x => x.ProductId == productId).Select(x => x.Quantity).SingleAsync());
    }

    [Fact]
    public async Task Unknown_operation_cross_user_replay_and_over_limit_batch_are_rejected()
    {
        await using var factory = new StockFlowApiFactory(postgres.Database.GetConnectionString());
        var firstUser = await LoginAsync(factory);
        var secondUser = await LoginAsync(factory);
        var admin = await LoginAdminAsync(factory);
        var supplierId = await CreateSupplierAsync(admin);
        var productId = await CreateProductAsync(admin);
        var operationId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();

        var unknown = await firstUser.PostAsJsonAsync("/api/sync", new
        {
            operations = new[] { new { clientOperationId = operationId, operationType = "DeleteEverything", resourceId, expectedVersion = 0L, payload = new { } } }
        });
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
        var malformed = await firstUser.PostAsJsonAsync("/api/sync", new
        {
            operations = new[] { new { clientOperationId = Guid.NewGuid(), operationType = "CreateReceipt", resourceId = Guid.NewGuid(), expectedVersion = 0L, payload = new { supplierId, lines = (object?)null } } }
        });
        Assert.Equal(HttpStatusCode.BadRequest, malformed.StatusCode);

        var first = await SendAsync(firstUser, operationId, resourceId, supplierId, productId, 10L);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var otherUserReplay = await SendAsync(secondUser, operationId, resourceId, supplierId, productId, 10L);
        Assert.Equal(HttpStatusCode.Conflict, otherUserReplay.StatusCode);

        var oversized = await firstUser.PostAsJsonAsync("/api/sync", new
        {
            operations = Enumerable.Range(0, 51).Select(_ => new { clientOperationId = Guid.NewGuid(), operationType = "CreateReceipt", resourceId = Guid.NewGuid(), expectedVersion = 0L, payload = new { supplierId, lines = new[] { new { productId, quantity = 1L, unitCostVnd = 1L } } } })
        });
        Assert.Equal(HttpStatusCode.BadRequest, oversized.StatusCode);
    }

    [Fact]
    public async Task Concurrent_retries_with_same_key_create_one_operation_and_one_draft()
    {
        await using var factory = new StockFlowApiFactory(postgres.Database.GetConnectionString());
        var warehouse = await LoginAsync(factory);
        var admin = await LoginAdminAsync(factory);
        var supplierId = await CreateSupplierAsync(admin);
        var productId = await CreateProductAsync(admin);
        var operationId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();

        var responses = await Task.WhenAll(
            SendAsync(warehouse, operationId, resourceId, supplierId, productId, 5L),
            SendAsync(warehouse, operationId, resourceId, supplierId, productId, 5L));
        Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
        var results = await Task.WhenAll(responses.Select(async response =>
            (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("results")[0].GetProperty("result").GetProperty("id").GetGuid()));
        Assert.All(results, id => Assert.Equal(resourceId, id));

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StockFlowDbContext>();
        Assert.Equal(1, await db.ClientOperations.CountAsync(operation => operation.ClientOperationId == operationId));
        Assert.Equal(1, await db.StockDocuments.CountAsync(document => document.Id == resourceId));
    }

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, Guid operationId, Guid resourceId, Guid supplierId, Guid productId, long quantity) =>
        client.PostAsJsonAsync("/api/sync", new
        {
            operations = new[] { new
            {
                clientOperationId = operationId,
                operationType = "CreateReceipt",
                resourceId,
                expectedVersion = 0L,
                payload = new { supplierId, lines = new[] { new { productId, quantity, unitCostVnd = 10L } } }
            } }
        });

    private static Task<HttpResponseMessage> SendUpdateAsync(HttpClient client, Guid operationId, Guid resourceId, Guid supplierId, Guid productId, long quantity) =>
        client.PostAsJsonAsync("/api/sync", new
        {
            operations = new[] { new
            {
                clientOperationId = operationId,
                operationType = "UpdateReceipt",
                resourceId,
                expectedVersion = 1L,
                payload = new { supplierId, lines = new[] { new { productId, quantity, unitCostVnd = 10L } } }
            } }
        });

    private static async Task<Guid> CreateProductAsync(HttpClient client)
    {
        var result = await client.PostAsJsonAsync("/api/products", new { sku = $"P-{Guid.NewGuid():N}", name = "Sản phẩm", unit = "cái", purchasePriceVnd = 1L, salePriceVnd = 1L, minimumStock = 0L });
        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        return (await result.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreateSupplierAsync(HttpClient client)
    {
        var result = await client.PostAsJsonAsync("/api/suppliers", new { code = $"S-{Guid.NewGuid():N}", name = "Nhà cung cấp" });
        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        return (await result.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private static async Task<HttpClient> LoginAsync(StockFlowApiFactory factory)
    {
        var email = $"warehouse-{Guid.NewGuid():N}@stockflow.test";
        var password = $"Valid!{Guid.NewGuid():N}";
        await factory.CreateUserAsync(email, password, "Warehouse");
        var client = factory.CreateClient();
        await AuthTests.RefreshCsrfAsync(client);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/auth/login", new { email, password })).StatusCode);
        await AuthTests.RefreshCsrfAsync(client);
        return client;
    }

    private static async Task<HttpClient> LoginAdminAsync(StockFlowApiFactory factory)
    {
        var email = $"admin-{Guid.NewGuid():N}@stockflow.test";
        var password = $"Valid!{Guid.NewGuid():N}";
        await factory.CreateUserAsync(email, password, "Admin");
        var client = factory.CreateClient();
        await AuthTests.RefreshCsrfAsync(client);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/auth/login", new { email, password })).StatusCode);
        await AuthTests.RefreshCsrfAsync(client);
        return client;
    }
}
