using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ProductCatalog.Tests;

public class ProductEndpointsTests : IDisposable
{
    // Each test gets its own host and singleton store.
    private readonly WebApplicationFactory<Program> _factory = new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder => builder.UseEnvironment("Development"));

    [Fact]
    public async Task Crud_round_trip_returns_expected_statuses_and_location()
    {
        using var client = _factory.CreateClient();
        var created = await client.PostAsJsonAsync("/api/products", new { name = "  Monitor  ", price = 3200000m, stock = 5 });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var product = await created.Content.ReadFromJsonAsync<ProductDto>();
        Assert.NotNull(product);
        Assert.Equal("Monitor", product.Name);
        Assert.True(product.Id > 2);
        Assert.NotNull(created.Headers.Location);
        Assert.Equal(product, await client.GetFromJsonAsync<ProductDto>(created.Headers.Location));
        var updated = await client.PutAsJsonAsync($"/api/products/{product.Id}", new { name = "Monitor 4K", price = 5000000m, stock = 0 });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var replacement = await updated.Content.ReadFromJsonAsync<ProductDto>();
        Assert.Equal(new ProductDto(product.Id, "Monitor 4K", 5000000m, 0), replacement);
        Assert.Equal(replacement, await client.GetFromJsonAsync<ProductDto>($"/api/products/{product.Id}"));
        var deleted = await client.DeleteAsync($"/api/products/{product.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal("", await deleted.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/products/{product.Id}")).StatusCode);
    }

    [Fact]
    public async Task List_and_query_binding_return_seed_data_and_case_insensitive_matches()
    {
        using var client = _factory.CreateClient();
        Assert.Equal(2, (await client.GetFromJsonAsync<ProductDto[]>("/api/products"))!.Length);
        var filtered = await client.GetFromJsonAsync<ProductDto[]>("/api/products?search=KEYBOARD");
        Assert.NotNull(filtered);
        Assert.Equal("Mechanical Keyboard", Assert.Single(filtered).Name);
        Assert.Empty((await client.GetFromJsonAsync<ProductDto[]>("/api/products?search=not-present"))!);
    }

    [Theory]
    [InlineData("", 10, 0, "name")]
    [InlineData("   ", 10, 0, "name")]
    [InlineData("Valid", 0, 0, "price")]
    [InlineData("Valid", -1, 0, "price")]
    [InlineData("Valid", 10, -1, "stock")]
    public async Task Invalid_create_is_rejected_before_store_mutation(string name, decimal price, int stock, string field)
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/products", new { name, price, stock });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var error = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(error.RootElement.GetProperty("errors").TryGetProperty(field, out _));
        Assert.Equal(2, (await client.GetFromJsonAsync<ProductDto[]>("/api/products"))!.Length);
    }

    [Fact]
    public async Task Long_name_and_invalid_update_do_not_change_store()
    {
        using var client = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/products", new { name = new string('x', 101), price = 1, stock = 0 })).StatusCode);
        var original = await client.GetFromJsonAsync<ProductDto>("/api/products/1");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/products/1", new { name = "", price = -1, stock = -1 })).StatusCode);
        Assert.Equal(original, await client.GetFromJsonAsync<ProductDto>("/api/products/1"));
    }

    [Theory]
    [InlineData("GET", "999", HttpStatusCode.NotFound)]
    [InlineData("PUT", "999", HttpStatusCode.NotFound)]
    [InlineData("DELETE", "999", HttpStatusCode.NotFound)]
    [InlineData("GET", "0", HttpStatusCode.BadRequest)]
    [InlineData("PUT", "-1", HttpStatusCode.BadRequest)]
    [InlineData("DELETE", "0", HttpStatusCode.BadRequest)]
    [InlineData("GET", "abc", HttpStatusCode.BadRequest)]
    public async Task Missing_or_invalid_ids_return_expected_status(string method, string id, HttpStatusCode expected)
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), $"/api/products/{id}");
        if (method == "PUT")
            request.Content = JsonContent.Create(new { name = "Valid", price = 1, stock = 0 });
        Assert.Equal(expected, (await client.SendAsync(request)).StatusCode);
    }

    [Fact]
    public async Task Swagger_exposes_crud_operations_in_development()
    {
        using var client = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/swagger/index.html")).StatusCode);
        using var document = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        var paths = document.RootElement.GetProperty("paths");
        Assert.True(paths.GetProperty("/api/products").TryGetProperty("post", out _));
        Assert.True(paths.GetProperty("/api/products/{id}").TryGetProperty("put", out _));
    }

    [Fact]
    public async Task Swagger_is_disabled_in_production()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/swagger/v1/swagger.json")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/products")).StatusCode);
    }

    public void Dispose() => _factory.Dispose();
    private sealed record ProductDto(int Id, string Name, decimal Price, int Stock);
}
