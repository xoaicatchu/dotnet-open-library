using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace GraphQL.Tests;

public class GraphQLTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public GraphQLTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<JsonElement> PostQueryAsync(string query)
    {
        var requestBody = JsonSerializer.Serialize(new { query });
        var response = await _client.PostAsync("/graphql", new StringContent(requestBody, Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var document = JsonDocument.Parse(content);
        return document.RootElement;
    }

    [Fact]
    public async Task Query_GetProducts_ReturnsArray()
    {
        var query = "{ products { id name price } }";
        var root = await PostQueryAsync(query);
        var products = root.GetProperty("data").GetProperty("products");
        products.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task Query_GetProductById_ReturnsProduct()
    {
        var query = "{ productById(id: 1) { id name } }";
        var root = await PostQueryAsync(query);
        var product = root.GetProperty("data").GetProperty("productById");
        product.ValueKind.Should().NotBe(JsonValueKind.Null);
        product.GetProperty("id").GetString().Should().Be("1");
    }

    [Fact]
    public async Task Query_GetProductById_NotFound_ReturnsNull()
    {
        var query = "{ productById(id: 9999) { id } }";
        var root = await PostQueryAsync(query);
        var product = root.GetProperty("data").GetProperty("productById");
        product.ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Query_FilterProducts_ReturnsFilteredList()
    {
        var query = "{ products(where: { price: { gt: 10 } }) { id price } }";
        var root = await PostQueryAsync(query);
        var products = root.GetProperty("data").GetProperty("products");
        products.ValueKind.Should().Be(JsonValueKind.Array);
        foreach (var product in products.EnumerateArray())
        {
            product.GetProperty("price").GetDecimal().Should().BeGreaterThan(10);
        }
    }

    [Fact]
    public async Task Query_SortProducts_ReturnsSorted()
    {
        var query = "{ products(order: { price: DESC }) { id price } }";
        var root = await PostQueryAsync(query);
        var products = root.GetProperty("data").GetProperty("products");
        products.ValueKind.Should().Be(JsonValueKind.Array);
        
        var enumerator = products.EnumerateArray();
        if (enumerator.MoveNext())
        {
            var firstPrice = enumerator.Current.GetProperty("price").GetDecimal();
            if (enumerator.MoveNext())
            {
                var secondPrice = enumerator.Current.GetProperty("price").GetDecimal();
                firstPrice.Should().BeGreaterThanOrEqualTo(secondPrice);
            }
        }
    }

    [Fact]
    public async Task Query_PagedProducts_ReturnsPagination()
    {
        var query = "{ productsPaged(first: 3) { nodes { id } totalCount } }";
        var root = await PostQueryAsync(query);
        var paged = root.GetProperty("data").GetProperty("productsPaged");
        var nodes = paged.GetProperty("nodes");
        nodes.GetArrayLength().Should().BeLessThanOrEqualTo(3);
        paged.GetProperty("totalCount").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Mutation_CreateProduct_ReturnsNewId()
    {
        var query = "mutation { createProduct(name: \"Test\", description: \"Desc\", price: 99, stock: 10, categoryId: 1) { id name } }";
        var root = await PostQueryAsync(query);
        var product = root.GetProperty("data").GetProperty("createProduct");
        int.Parse(product.GetProperty("id").GetString()!).Should().BeGreaterThan(0);
        product.GetProperty("name").GetString().Should().Be("Test");
    }

    [Fact]
    public async Task Mutation_UpdateProduct_ReturnsUpdatedPrice()
    {
        var query = "mutation { updateProduct(id: 1, price: 999.99) { id price } }";
        var root = await PostQueryAsync(query);
        var product = root.GetProperty("data").GetProperty("updateProduct");
        product.GetProperty("id").GetString().Should().Be("1");
        product.GetProperty("price").GetDecimal().Should().Be(999.99m);
    }

    [Fact]
    public async Task Mutation_CreateOrder_ReturnsTotalPrice()
    {
        // Must ensure product stock is sufficient, let's create a product first or just use ID 1
        var createProduct = "mutation { createProduct(name: \"P2\", description: \"Desc\", price: 100, stock: 50, categoryId: 1) { id } }";
        var rootCreate = await PostQueryAsync(createProduct);
        var newProductId = int.Parse(rootCreate.GetProperty("data").GetProperty("createProduct").GetProperty("id").GetString()!);

        var query = $"mutation {{ createOrder(customerName: \"John Doe\", productId: {newProductId}, quantity: 2) {{ id totalPrice }} }}";
        var root = await PostQueryAsync(query);
        var order = root.GetProperty("data").GetProperty("createOrder");
        order.GetProperty("totalPrice").GetDecimal().Should().Be(200m);
    }

    [Fact]
    public async Task Mutation_DeleteProduct_ReturnsTrue()
    {
        // Create a product to delete
        var createProduct = "mutation { createProduct(name: \"ToDelete\", description: \"Desc\", price: 100, stock: 50, categoryId: 1) { id } }";
        var rootCreate = await PostQueryAsync(createProduct);
        var newProductId = int.Parse(rootCreate.GetProperty("data").GetProperty("createProduct").GetProperty("id").GetString()!);

        var query = $"mutation {{ deleteProduct(id: {newProductId}) }}";
        var root = await PostQueryAsync(query);
        var success = root.GetProperty("data").GetProperty("deleteProduct").GetBoolean();
        success.Should().BeTrue();
    }
}
