using System.Net;
using System.Net.Http.Json;
using AutoMapper;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using OrderMapping.Api.Mappings;
using OrderMapping.Api.Models;

namespace OrderMapping.Tests;

public class AutoMapperConfigurationTests
{
    [Fact]
    public void Configuration_IsValid()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<OrderMappingProfile>();
        }, NullLoggerFactory.Instance);

        // Verifies that all mapped types are valid and no required mappings are missing
        config.AssertConfigurationIsValid();
    }
}

public class OrderMappingApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public OrderMappingApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_ReturnsMappedSummariesWithCalculatedTotals()
    {
        var response = await _client.GetAsync("/api/orders");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var orders = await response.Content.ReadFromJsonAsync<List<OrderSummaryDto>>();
        Assert.NotNull(orders);
        Assert.NotEmpty(orders);

        var first = orders.First();
        Assert.Equal("Alice Smith", first.CustomerFullName);
        // TotalAmount: (25.50 * 2) + (45.00 * 1) = 51.00 + 45.00 = 96.00
        Assert.Equal(96.00m, first.TotalAmount);
        Assert.Equal(3, first.ItemsCount);
    }

    [Fact]
    public async Task GetById_ReturnsDetailedMappedDtoWithNestedObjects()
    {
        var response = await _client.GetAsync("/api/orders/1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var order = await response.Content.ReadFromJsonAsync<OrderDetailDto>();
        Assert.NotNull(order);
        Assert.Equal(1, order.Id);
        Assert.Equal("Alice Smith", order.Customer.FullName);
        Assert.Equal("Seattle", order.ShippingAddress.City);
        Assert.Equal(2, order.Items.Count);

        var firstItem = order.Items.First(i => i.ProductName == "Wireless Mouse");
        Assert.Equal(25.50m, firstItem.UnitPrice);
        Assert.Equal(2, firstItem.Quantity);
        Assert.Equal(51.00m, firstItem.LineTotal);
    }

    [Fact]
    public async Task Create_MapsRequestToEntityAndReturnsCreatedDto()
    {
        var request = new CreateOrderRequest
        {
            Customer = new CreateCustomerRequest("Bob", "Marley", "bob@example.com"),
            ShippingAddress = new AddressDto("100 Reggae Way", "Kingston", "JM", "12345", "Jamaica"),
            Items = new List<CreateOrderItemRequest>
            {
                new("Acoustic Guitar", 350.00m, 1),
                new("Guitar Picks 10-pack", 9.99m, 2)
            }
        };

        var response = await _client.PostAsJsonAsync("/api/orders", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<OrderDetailDto>();
        Assert.NotNull(created);
        Assert.True(created.Id > 0);
        Assert.Equal("Bob Marley", created.Customer.FullName);
        Assert.Equal("Kingston", created.ShippingAddress.City);
        Assert.Equal(2, created.Items.Count);
        // Total: 350.00 + (9.99 * 2) = 369.98
        Assert.Equal(369.98m, created.TotalAmount);
    }

    [Fact]
    public async Task UpdateAddress_MapsRequestOntoExistingAddressEntity()
    {
        var updateRequest = new UpdateAddressRequest
        {
            Street = "999 Innovation Blvd",
            City = "San Francisco",
            State = "CA",
            ZipCode = "94105",
            Country = "USA"
        };

        var response = await _client.PutAsJsonAsync("/api/orders/1/address", updateRequest);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updatedAddress = await response.Content.ReadFromJsonAsync<AddressDto>();
        Assert.NotNull(updatedAddress);
        Assert.Equal("San Francisco", updatedAddress.City);
        Assert.Equal("999 Innovation Blvd", updatedAddress.Street);
    }

    [Fact]
    public async Task GetById_WithNonExistentId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/orders/9999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithEmptyCustomer_ReturnsBadRequest()
    {
        var request = new CreateOrderRequest
        {
            Customer = new CreateCustomerRequest("", "", "test@example.com"),
            ShippingAddress = new AddressDto("Street", "City", "State", "12345", "Country"),
            Items = new List<CreateOrderItemRequest> { new("Item", 10m, 1) }
        };

        var response = await _client.PostAsJsonAsync("/api/orders", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
