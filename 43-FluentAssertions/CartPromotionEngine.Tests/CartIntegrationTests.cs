using System.Net;
using System.Net.Http.Json;
using CartPromotionEngine.Api.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CartPromotionEngine.Tests;

public class CartIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public CartIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetCart_Returns200WithEmptyCart()
    {
        // Arrange
        var cartId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/cart/{cartId}");

        // Assert (FluentAssertions on HttpResponse & Deserialized object)
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var cart = await response.Content.ReadFromJsonAsync<ShoppingCart>();
        cart.Should().NotBeNull();
        cart!.Id.Should().Be(cartId);
        cart.Items.Should().BeEmpty();
        cart.TotalAmount.Should().Be(0m);
    }

    [Fact]
    public async Task AddItem_And_ApplyCoupon_And_Checkout_EndToEndFlow()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var addItemRequest = new AddItemRequest(
            ProductId: Guid.NewGuid(),
            ProductName: "4K Monitor",
            UnitPrice: 300.00m,
            Quantity: 1
        );

        // Act 1: Add Item
        var addResponse = await _client.PostAsJsonAsync($"/api/cart/{cartId}/items", addItemRequest);
        addResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var cartAfterAdd = await addResponse.Content.ReadFromJsonAsync<ShoppingCart>();
        cartAfterAdd.Should().NotBeNull();
        cartAfterAdd!.Items.Should().HaveCount(1);
        cartAfterAdd.SubTotal.Should().Be(300.00m);

        // Act 2: Apply Coupon
        var couponResponse = await _client.PostAsJsonAsync($"/api/cart/{cartId}/coupon", new ApplyCouponRequest("VIP20"));
        couponResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var cartAfterCoupon = await couponResponse.Content.ReadFromJsonAsync<ShoppingCart>();
        cartAfterCoupon.Should().NotBeNull();
        cartAfterCoupon!.Coupon.Should().NotBeNull();
        cartAfterCoupon.Coupon!.Code.Should().Be("VIP20");
        cartAfterCoupon.DiscountTotal.Should().Be(60.00m); // 20% of 300
        cartAfterCoupon.TotalAmount.Should().Be(240.00m);

        // Act 3: Checkout
        var checkoutResponse = await _client.PostAsJsonAsync($"/api/cart/{cartId}/checkout", new CheckoutRequest("123 Street", "CreditCard"));
        checkoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var checkoutResult = await checkoutResponse.Content.ReadFromJsonAsync<CheckoutResult>();
        checkoutResult.Should().NotBeNull();
        checkoutResult!.Status.Should().Be("Completed");
        checkoutResult.TotalPaid.Should().Be(240.00m);
        checkoutResult.ItemsCount.Should().Be(1);
        checkoutResult.OrderNumber.Should().StartWith("ORD-");
    }

    [Fact]
    public async Task ApplyCoupon_InvalidCode_ReturnsBadRequest()
    {
        // Arrange
        var cartId = Guid.NewGuid();

        // Act
        var response = await _client.PostAsJsonAsync($"/api/cart/{cartId}/coupon", new ApplyCouponRequest("NOT_EXISTING"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Checkout_EmptyCart_ReturnsBadRequest()
    {
        // Arrange
        var cartId = Guid.NewGuid();

        // Act
        var response = await _client.PostAsJsonAsync($"/api/cart/{cartId}/checkout", new CheckoutRequest("123 Street", "CreditCard"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
