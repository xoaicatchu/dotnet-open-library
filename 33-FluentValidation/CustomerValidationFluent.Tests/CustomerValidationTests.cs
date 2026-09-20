using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using CustomerValidationFluent.Api.Models;

namespace CustomerValidationFluent.Tests;

public class CustomerValidationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public CustomerValidationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ValidCustomer_ReturnsCreated()
    {
        var request = new CustomerRegistrationRequest(
            FullName: "Sarah Connor",
            Email: "sarah.connor@example.com",
            Age: 32,
            Address: new AddressDto("456 Resistance Way", "Los Angeles", "90001", "USA"),
            IsVip: false
        );

        var response = await _client.PostAsJsonAsync("/api/customers/register", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<CustomerResponse>();
        Assert.NotNull(result);
        Assert.Equal("Sarah Connor", result.FullName);
        Assert.Equal("sarah.connor@example.com", result.Email);
    }

    [Fact]
    public async Task Register_InvalidEmail_ReturnsBadRequestWithEmailError()
    {
        var request = new CustomerRegistrationRequest(
            FullName: "Invalid Email User",
            Email: "not-an-email",
            Age: 25,
            Address: new AddressDto("123 Street", "Hanoi", "100000", "Vietnam")
        );

        var response = await _client.PostAsJsonAsync("/api/customers/register", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(problem.Errors.ContainsKey("Email"));
    }

    [Fact]
    public async Task Register_UnderageCustomer_ReturnsBadRequestWithAgeError()
    {
        var request = new CustomerRegistrationRequest(
            FullName: "Minor User",
            Email: "minor@example.com",
            Age: 16,
            Address: new AddressDto("123 Street", "Hanoi", "100000", "Vietnam")
        );

        var response = await _client.PostAsJsonAsync("/api/customers/register", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(problem.Errors.ContainsKey("Age"));
    }

    [Fact]
    public async Task Register_ShortFullName_ReturnsBadRequestWithFullNameError()
    {
        var request = new CustomerRegistrationRequest(
            FullName: "Ab",
            Email: "ab@example.com",
            Age: 20,
            Address: new AddressDto("123 Street", "Hanoi", "100000", "Vietnam")
        );

        var response = await _client.PostAsJsonAsync("/api/customers/register", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(problem.Errors.ContainsKey("FullName"));
    }

    [Fact]
    public async Task Register_VipCustomerWithoutMembershipNumber_ReturnsBadRequest()
    {
        var request = new CustomerRegistrationRequest(
            FullName: "VIP Without ID",
            Email: "vip@example.com",
            Age: 40,
            Address: new AddressDto("123 Luxury Ave", "London", "12345", "UK"),
            IsVip: true,
            MembershipNumber: null
        );

        var response = await _client.PostAsJsonAsync("/api/customers/register", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(problem.Errors.ContainsKey("MembershipNumber"));
    }

    [Fact]
    public async Task Register_VipCustomerWithValidMembership_ReturnsCreated()
    {
        var request = new CustomerRegistrationRequest(
            FullName: "VIP Valid Member",
            Email: "vip.valid@example.com",
            Age: 45,
            Address: new AddressDto("123 Luxury Ave", "London", "12345", "UK"),
            IsVip: true,
            MembershipNumber: "VIP-9999"
        );

        var response = await _client.PostAsJsonAsync("/api/customers/register", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<CustomerResponse>();
        Assert.NotNull(result);
        Assert.True(result.IsVip);
        Assert.Equal("VIP-9999", result.MembershipNumber);
    }

    [Fact]
    public async Task Register_InvalidAddress_ReturnsBadRequestWithNestedPropertyErrors()
    {
        var request = new CustomerRegistrationRequest(
            FullName: "Bad Address User",
            Email: "address@example.com",
            Age: 30,
            Address: new AddressDto("", "", "INVALID_POSTAL", "")
        );

        var response = await _client.PostAsJsonAsync("/api/customers/register", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(problem.Errors.ContainsKey("Address.Street"));
        Assert.True(problem.Errors.ContainsKey("Address.City"));
        Assert.True(problem.Errors.ContainsKey("Address.PostalCode"));
        Assert.True(problem.Errors.ContainsKey("Address.Country"));
    }

    [Fact]
    public async Task CreateOrder_ValidRequest_ReturnsOkWithCalculatedTotal()
    {
        var request = new CreateOrderRequest(
            CustomerEmail: "customer@example.com",
            ShippingMethod: "Express",
            DiscountPercent: 10,
            Items: new List<OrderItemDto>
            {
                new("SKU-100", "Gaming Mouse", 2, 50.00m), // 100
                new("SKU-200", "Mechanical Keyboard", 1, 100.00m) // 100 -> Total 200 - 10% = 180
            }
        );

        var response = await _client.PostAsJsonAsync("/api/customers/orders", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.NotNull(order);
        Assert.Equal(180.00m, order.TotalAmount);
        Assert.Equal("Express", order.ShippingMethod);
        Assert.StartsWith("ORD-", order.OrderId);
    }

    [Fact]
    public async Task CreateOrder_InvalidItems_ReturnsBadRequestWithCollectionIndexErrors()
    {
        var request = new CreateOrderRequest(
            CustomerEmail: "customer@example.com",
            ShippingMethod: "Standard",
            DiscountPercent: 0,
            Items: new List<OrderItemDto>
            {
                new("bad sku!", "", 0, -10.00m)
            }
        );

        var response = await _client.PostAsJsonAsync("/api/customers/orders", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(problem.Errors.ContainsKey("Items[0].Sku"));
        Assert.True(problem.Errors.ContainsKey("Items[0].Name"));
        Assert.True(problem.Errors.ContainsKey("Items[0].Quantity"));
        Assert.True(problem.Errors.ContainsKey("Items[0].UnitPrice"));
    }

    [Fact]
    public async Task CreateOrder_InvalidShippingMethod_ReturnsBadRequest()
    {
        var request = new CreateOrderRequest(
            CustomerEmail: "customer@example.com",
            ShippingMethod: "Teleportation",
            DiscountPercent: 0,
            Items: new List<OrderItemDto>
            {
                new("SKU-100", "Gaming Mouse", 1, 50.00m)
            }
        );

        var response = await _client.PostAsJsonAsync("/api/customers/orders", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(problem.Errors.ContainsKey("ShippingMethod"));
    }

    [Fact]
    public async Task GetById_ExistingCustomer_ReturnsOk()
    {
        var request = new CustomerRegistrationRequest(
            FullName: "John Wick",
            Email: "john.wick@continental.com",
            Age: 40,
            Address: new AddressDto("Continental Hotel", "New York", "10001", "USA")
        );

        var createRes = await _client.PostAsJsonAsync("/api/customers/register", request);
        var created = await createRes.Content.ReadFromJsonAsync<CustomerResponse>();
        Assert.NotNull(created);

        var getRes = await _client.GetAsync($"/api/customers/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getRes.StatusCode);

        var retrieved = await getRes.Content.ReadFromJsonAsync<CustomerResponse>();
        Assert.NotNull(retrieved);
        Assert.Equal(created.Id, retrieved.Id);
        Assert.Equal("John Wick", retrieved.FullName);
    }

    [Fact]
    public async Task GetById_NonExistentCustomer_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/customers/999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
