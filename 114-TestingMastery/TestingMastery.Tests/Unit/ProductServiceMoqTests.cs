using Bogus;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TestingMastery.Api.Controllers;
using TestingMastery.Api.Services;

namespace TestingMastery.Tests.Unit;

public class ProductServiceMoqTests
{
    [Fact]
    public async Task GetAll_ReturnsOkWithProducts()
    {
        var expected = new List<ProductDto> { new(1, "Product", 10m, 5) };
        var mockService = new Mock<IProductService>();
        mockService.Setup(s => s.GetAllAsync()).ReturnsAsync(expected);
        var controller = new ProductsController(mockService.Object);
        
        var result = await controller.GetAll();
        
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expected);
        mockService.Verify(s => s.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetById_WhenExists_ReturnsProduct()
    {
        // Arrange
        var expectedProduct = new ProductDto(1, "Test Product", 99.99m, 10);
        var mockService = new Mock<IProductService>();
        mockService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(expectedProduct);
        var controller = new ProductsController(mockService.Object);
        
        // Act
        var result = await controller.GetById(1);
        
        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var product = okResult.Value.Should().BeOfType<ProductDto>().Subject;
        product.Id.Should().Be(1);
        product.Name.Should().Be("Test Product");
        mockService.Verify(s => s.GetByIdAsync(1), Times.Once);
    }
    
    [Fact]
    public async Task GetById_WhenNotExists_Returns404()
    {
        var mockService = new Mock<IProductService>();
        mockService.Setup(s => s.GetByIdAsync(999)).ReturnsAsync((ProductDto?)null);
        var controller = new ProductsController(mockService.Object);
        
        var result = await controller.GetById(999);
        result.Result.Should().BeOfType<NotFoundResult>();
    }
    
    [Fact]
    public async Task Create_WithFakeData_ReturnsCreated()
    {
        var faker = new Faker<CreateProductRequest>()
            .CustomInstantiator(f => new CreateProductRequest(
                f.Commerce.ProductName(),
                Math.Round(f.Random.Decimal(10, 500), 2),
                f.Random.Int(1, 100)));
        var request = faker.Generate();
        var mockService = new Mock<IProductService>();
        mockService.Setup(s => s.CreateAsync(It.IsAny<CreateProductRequest>()))
            .ReturnsAsync(new ProductDto(1, request.Name, request.Price, request.Stock));
        var controller = new ProductsController(mockService.Object);
        
        var result = await controller.Create(request);
        result.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task Update_WhenExists_ReturnsOk()
    {
        var request = new UpdateProductRequest("Updated", 50m, 20);
        var mockService = new Mock<IProductService>();
        mockService.Setup(s => s.UpdateAsync(1, request)).ReturnsAsync(new ProductDto(1, "Updated", 50m, 20));
        var controller = new ProductsController(mockService.Object);
        
        var result = await controller.Update(1, request);
        
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var product = okResult.Value.Should().BeOfType<ProductDto>().Subject;
        product.Name.Should().Be("Updated");
    }
}
