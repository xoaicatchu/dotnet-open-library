using CartPromotionEngine.Api.Models;
using CartPromotionEngine.Api.Services;
using FluentAssertions;
using Xunit;

namespace CartPromotionEngine.Tests;

public class CartUnitTests
{
    private readonly CartService _service = new();

    [Fact]
    public void AddItem_SingleItem_CalculatesSubTotalAndTotalCorrectly()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var request = new AddItemRequest(
            ProductId: Guid.NewGuid(),
            ProductName: "Mechanical Keyboard",
            UnitPrice: 45.00m,
            Quantity: 2
        );

        // Act
        var cart = _service.AddItem(cartId, request);

        // Assert (FluentAssertions: BeEquivalentTo, ContainSingle, BeCloseTo)
        cart.Should().NotBeNull();
        cart.Id.Should().Be(cartId);
        cart.Items.Should().ContainSingle();

        var item = cart.Items.First();
        item.ProductName.Should().Be("Mechanical Keyboard");
        item.Quantity.Should().Be(2);
        item.TotalPrice.Should().Be(90.00m);

        cart.SubTotal.Should().Be(90.00m);
        cart.DiscountTotal.Should().Be(0m);
        cart.TotalAmount.Should().Be(90.00m);
        cart.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void AddItem_DuplicateProduct_IncrementsQuantityAndRecalculates()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        _service.AddItem(cartId, new AddItemRequest(productId, "USB-C Cable", 15.00m, 1));

        // Act
        var cart = _service.AddItem(cartId, new AddItemRequest(productId, "USB-C Cable", 15.00m, 2));

        // Assert (FluentAssertions: HaveCount, Match)
        cart.Items.Should().HaveCount(1);
        cart.Items.Should().ContainSingle(i => i.Quantity == 3 && i.TotalPrice == 45.00m);
        cart.SubTotal.Should().Be(45.00m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddItem_InvalidQuantity_ThrowsArgumentOutOfRangeException(int invalidQuantity)
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var request = new AddItemRequest(Guid.NewGuid(), "Item", 10.00m, invalidQuantity);

        // Act
        Action act = () => _service.AddItem(cartId, request);

        // Assert (FluentAssertions: Throw, WithParameterName)
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("Quantity");
    }

    [Fact]
    public void ApplyCoupon_ValidCode_CalculatesDiscountCorrectly()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        _service.AddItem(cartId, new AddItemRequest(Guid.NewGuid(), "Monitor", 200.00m, 1));

        // Act
        var cart = _service.ApplyCoupon(cartId, "SAVE10");

        // Assert (FluentAssertions: chained assertions, BeInRange)
        cart.Coupon.Should().NotBeNull();
        cart.Coupon!.Code.Should().Be("SAVE10");
        cart.Coupon.DiscountPercent.Should().Be(0.10m);
        cart.DiscountTotal.Should().Be(20.00m);
        cart.TotalAmount.Should().Be(180.00m);
        cart.DiscountTotal.Should().BeInRange(10m, 50m);
    }

    [Fact]
    public void ApplyCoupon_UnknownCode_ThrowsArgumentException()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        _service.AddItem(cartId, new AddItemRequest(Guid.NewGuid(), "Headphones", 50.00m, 1));

        // Act
        Action act = () => _service.ApplyCoupon(cartId, "INVALID_CODE_999");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid or expired coupon*");
    }

    [Fact]
    public void ApplyCoupon_UnderMinSpend_ThrowsInvalidOperationException()
    {
        // Arrange (VIP20 requires minimum order of $100)
        var cartId = Guid.NewGuid();
        _service.AddItem(cartId, new AddItemRequest(Guid.NewGuid(), "Mousepad", 15.00m, 1));

        // Act
        Action act = () => _service.ApplyCoupon(cartId, "VIP20");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*requires a minimum order*");
    }

    [Fact]
    public void Checkout_EmptyCart_ThrowsInvalidOperationException()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var request = new CheckoutRequest("123 Street", "CreditCard");

        // Act
        Action act = () => _service.Checkout(cartId, request);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*empty*");
    }

    [Fact]
    public void Checkout_ValidCart_ResetsCartAndReturnsCompletedStatus()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        _service.AddItem(cartId, new AddItemRequest(Guid.NewGuid(), "Laptop Stand", 60.00m, 1));
        _service.ApplyCoupon(cartId, "SAVE10");

        var request = new CheckoutRequest("456 Avenue", "PayPal");

        // Act
        var result = _service.Checkout(cartId, request);

        // Assert (FluentAssertions: StartWith, HaveLength, BeEmpty)
        result.Status.Should().Be("Completed");
        result.TotalPaid.Should().Be(54.00m);
        result.ItemsCount.Should().Be(1);
        result.OrderNumber.Should().StartWith("ORD-");

        var clearedCart = _service.GetCart(cartId);
        clearedCart.Items.Should().BeEmpty();
        clearedCart.TotalAmount.Should().Be(0m);
    }
}
