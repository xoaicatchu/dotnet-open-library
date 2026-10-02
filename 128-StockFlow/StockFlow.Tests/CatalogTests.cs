using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using StockFlow.Tests.Infrastructure;

namespace StockFlow.Tests;

[Collection("Postgres")]
public sealed class CatalogTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Admin_can_create_product_and_duplicate_normalized_sku_is_rejected()
    {
        await using var factory = new StockFlowApiFactory(postgres.Database.GetConnectionString());
        var client = await LoginAsync(factory, "Admin");
        var product = new
        {
            sku = $"  paper-{Guid.NewGuid():N}  ",
            barcode = "  ",
            name = "Giấy A4",
            unit = "ram",
            purchasePriceVnd = 45_000L,
            salePriceVnd = 55_000L,
            minimumStock = 10L
        };

        var created = await client.PostAsJsonAsync("/api/products", product);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var body = await created.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(product.sku.Trim().ToUpperInvariant(), body.GetProperty("sku").GetString());
        Assert.Null(body.GetProperty("barcode").GetString());
        Assert.Equal(0, body.GetProperty("quantity").GetInt64());

        var duplicate = await client.PostAsJsonAsync("/api/products", new
        {
            sku = product.sku.Trim().ToUpperInvariant(),
            product.barcode,
            product.name,
            product.unit,
            product.purchasePriceVnd,
            product.salePriceVnd,
            product.minimumStock
        });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task Viewer_cannot_create_product_and_admin_can_create_supplier()
    {
        await using var factory = new StockFlowApiFactory(postgres.Database.GetConnectionString());
        var viewer = await LoginAsync(factory, "Viewer");
        var product = new { sku = $"SKU-{Guid.NewGuid():N}", name = "Hàng", unit = "cái", purchasePriceVnd = 0L, salePriceVnd = 0L, minimumStock = 0L };
        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.PostAsJsonAsync("/api/products", product)).StatusCode);

        var admin = await LoginAsync(factory, "Admin");
        var supplier = new { code = $"SUP-{Guid.NewGuid():N}", name = "Nhà cung cấp Việt", phone = "0900000000" };
        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync("/api/suppliers", supplier)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync("/api/suppliers", supplier)).StatusCode);
    }

    [Fact]
    public async Task Update_normalizes_codes_and_rejects_another_products_sku()
    {
        await using var factory = new StockFlowApiFactory(postgres.Database.GetConnectionString());
        var admin = await LoginAsync(factory, "Admin");
        var firstSku = $"SKU-{Guid.NewGuid():N}";
        var first = await admin.PostAsJsonAsync("/api/products", new { sku = firstSku, name = "Đầu tiên", unit = "cái", purchasePriceVnd = 0L, salePriceVnd = 0L, minimumStock = 0L });
        var second = await admin.PostAsJsonAsync("/api/products", new { sku = $"OTHER-{Guid.NewGuid():N}", name = "Thứ hai", unit = "cái", purchasePriceVnd = 0L, salePriceVnd = 0L, minimumStock = 0L });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        var secondId = (await second.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var collision = await admin.PutAsJsonAsync($"/api/products/{secondId}", new
        {
            sku = $"  {firstSku.ToLowerInvariant()}  ",
            barcode = "  ",
            name = "Thứ hai",
            unit = "cái",
            purchasePriceVnd = 0L,
            salePriceVnd = 0L,
            minimumStock = 0L,
            isActive = true
        });
        Assert.Equal(HttpStatusCode.Conflict, collision.StatusCode);
        Assert.Equal((int)HttpStatusCode.Conflict, (await collision.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetInt32());

        var deactivate = await admin.PutAsJsonAsync($"/api/products/{secondId}", new
        {
            sku = $"OTHER-{Guid.NewGuid():N}",
            name = "Thứ hai",
            unit = "cái",
            purchasePriceVnd = 0L,
            salePriceVnd = 0L,
            minimumStock = 0L,
            isActive = false
        });
        Assert.Equal(HttpStatusCode.OK, deactivate.StatusCode);
        var stillVisible = await admin.GetAsync($"/api/products/{secondId}");
        Assert.Equal(HttpStatusCode.OK, stillVisible.StatusCode);
        Assert.False((await stillVisible.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("isActive").GetBoolean());

        var supplierResponse = await admin.PostAsJsonAsync("/api/suppliers", new { code = $"S-{Guid.NewGuid():N}", name = "Nhà cung cấp" });
        Assert.Equal(HttpStatusCode.Created, supplierResponse.StatusCode);
        var supplierId = (await supplierResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var warehouse = await LoginAsync(factory, "Warehouse");
        var draft = await warehouse.PostAsJsonAsync("/api/documents", new
        {
            id = Guid.NewGuid(),
            expectedVersion = 0L,
            type = "Receipt",
            supplierId,
            lines = new[] { new { productId = secondId, quantity = 1L, unitCostVnd = 1L } }
        });
        Assert.Equal(HttpStatusCode.Conflict, draft.StatusCode);
    }

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
