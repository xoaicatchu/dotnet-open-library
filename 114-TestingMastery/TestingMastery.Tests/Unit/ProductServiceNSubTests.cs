using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using TestingMastery.Api.Controllers;
using TestingMastery.Api.Services;

namespace TestingMastery.Tests.Unit;

public class ProductServiceNSubTests
{
    [Fact]
    public async Task GetAll_CallsService_ReturnsProducts()
    {
        // NSubstitute syntax
        var service = Substitute.For<IProductService>();
        var expected = new List<ProductDto> { new(1, "Product", 10m, 5) };
        service.GetAllAsync().Returns(expected);
        var controller = new ProductsController(service);
        
        var result = await controller.GetAll();
        
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expected);
        await service.Received(1).GetAllAsync();
    }
    
    [Fact]
    public async Task Delete_WhenExists_Returns204()
    {
        var service = Substitute.For<IProductService>();
        service.DeleteAsync(1).Returns(true);
        var controller = new ProductsController(service);
        
        var result = await controller.Delete(1);
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_WhenNotExists_Returns404()
    {
        var service = Substitute.For<IProductService>();
        service.DeleteAsync(999).Returns(false);
        var controller = new ProductsController(service);
        
        var result = await controller.Delete(999);
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Update_WhenNotExists_Returns404()
    {
        var service = Substitute.For<IProductService>();
        var request = new UpdateProductRequest("Not Found", 0, 0);
        service.UpdateAsync(999, request).Returns((ProductDto?)null);
        var controller = new ProductsController(service);
        
        var result = await controller.Update(999, request);
        result.Result.Should().BeOfType<NotFoundResult>();
    }
}
