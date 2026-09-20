using System.Net;
using System.Net.Http.Json;
using FakeDataGenerator.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace FakeDataGenerator.Tests;

public class MockDataTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public MockDataTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetUsers_Default_Returns10Users()
    {
        // Act
        var response = await _client.GetAsync("/api/mock/users");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var users = await response.Content.ReadFromJsonAsync<List<UserProfile>>();
        Assert.NotNull(users);
        Assert.Equal(10, users.Count);
        Assert.All(users, u =>
        {
            Assert.NotEqual(Guid.Empty, u.Id);
            Assert.False(string.IsNullOrWhiteSpace(u.FirstName));
            Assert.False(string.IsNullOrWhiteSpace(u.LastName));
            Assert.False(string.IsNullOrWhiteSpace(u.Email));
            Assert.Contains("@", u.Email);
            Assert.NotNull(u.Address);
            Assert.False(string.IsNullOrWhiteSpace(u.Address.City));
        });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(101)]
    public async Task GetUsers_InvalidCount_ReturnsBadRequest(int invalidCount)
    {
        // Act
        var response = await _client.GetAsync($"/api/mock/users?count={invalidCount}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_DeterministicSeed_ReturnsIdenticalUsers()
    {
        // Act
        var res1 = await _client.GetFromJsonAsync<List<UserProfile>>("/api/mock/users?count=5&seed=999");
        var res2 = await _client.GetFromJsonAsync<List<UserProfile>>("/api/mock/users?count=5&seed=999");

        // Assert
        Assert.NotNull(res1);
        Assert.NotNull(res2);
        Assert.Equal(res1.Count, res2.Count);

        for (int i = 0; i < res1.Count; i++)
        {
            Assert.Equal(res1[i].FullName, res2[i].FullName);
            Assert.Equal(res1[i].Email, res2[i].Email);
            Assert.Equal(res1[i].Address.Street, res2[i].Address.Street);
        }
    }

    [Fact]
    public async Task GetUsers_DifferentSeeds_ReturnsDifferentUsers()
    {
        // Act
        var res1 = await _client.GetFromJsonAsync<List<UserProfile>>("/api/mock/users?count=5&seed=111");
        var res2 = await _client.GetFromJsonAsync<List<UserProfile>>("/api/mock/users?count=5&seed=222");

        // Assert
        Assert.NotNull(res1);
        Assert.NotNull(res2);
        Assert.NotEqual(res1[0].FullName, res2[0].FullName);
    }

    [Fact]
    public async Task GetUsers_VietnameseLocale_ReturnsPopulatedUsers()
    {
        // Act
        var response = await _client.GetAsync("/api/mock/users?count=5&locale=vi");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var users = await response.Content.ReadFromJsonAsync<List<UserProfile>>();
        Assert.NotNull(users);
        Assert.Equal(5, users.Count);
        Assert.All(users, u =>
        {
            Assert.False(string.IsNullOrWhiteSpace(u.FullName));
            Assert.False(string.IsNullOrWhiteSpace(u.Email));
        });
    }

    [Fact]
    public async Task GetOrders_ReturnsValidOrdersWithItemsAndCorrectTotals()
    {
        // Act
        var response = await _client.GetAsync("/api/mock/orders?count=4");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var orders = await response.Content.ReadFromJsonAsync<List<CustomerOrder>>();
        Assert.NotNull(orders);
        Assert.Equal(4, orders.Count);

        foreach (var order in orders)
        {
            Assert.StartsWith("ORD-", order.OrderNumber);
            Assert.NotEmpty(order.Items);
            Assert.True(order.TotalAmount > 0);

            decimal expectedTotal = 0;
            foreach (var item in order.Items)
            {
                Assert.Equal(Math.Round(item.UnitPrice * item.Quantity, 2), item.TotalPrice);
                expectedTotal += item.TotalPrice;
            }

            Assert.Equal(expectedTotal, order.TotalAmount);
        }
    }

    [Fact]
    public async Task GetOrders_InvalidCount_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/mock/orders?count=51");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetCustomerById_Deterministic_ReturnsSameData()
    {
        // Arrange
        var customerId = Guid.NewGuid();

        // Act
        var res1 = await _client.GetFromJsonAsync<UserProfile>($"/api/mock/customers/{customerId}");
        var res2 = await _client.GetFromJsonAsync<UserProfile>($"/api/mock/customers/{customerId}");

        // Assert
        Assert.NotNull(res1);
        Assert.NotNull(res2);
        Assert.Equal(customerId, res1.Id);
        Assert.Equal(customerId, res2.Id);
        Assert.Equal(res1.FullName, res2.FullName);
        Assert.Equal(res1.Email, res2.Email);
    }

    [Fact]
    public async Task SeedDatabase_ValidRequest_ReturnsSummary()
    {
        // Arrange
        var request = new SeedDatabaseRequest(NumberOfUsers: 20, NumberOfOrders: 15, Seed: 42);

        // Act
        var response = await _client.PostAsJsonAsync("/api/mock/seed-db", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<SeedDatabaseResponse>();
        Assert.NotNull(result);
        Assert.Equal(20, result.UsersCreated);
        Assert.Equal(15, result.OrdersCreated);
        Assert.NotNull(result.SampleUser);
        Assert.NotNull(result.SampleOrder);
        Assert.True(result.ExecutionTimeMs >= 0);
    }

    [Fact]
    public async Task SeedDatabase_InvalidUserCount_ReturnsBadRequest()
    {
        // Arrange
        var request = new SeedDatabaseRequest(NumberOfUsers: 0, NumberOfOrders: 10);

        // Act
        var response = await _client.PostAsJsonAsync("/api/mock/seed-db", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
