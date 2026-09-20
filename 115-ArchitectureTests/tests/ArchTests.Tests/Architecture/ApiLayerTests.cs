using ArchTests.Api.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NetArchTest.Rules;
using Xunit;
using System.Reflection;

namespace ArchTests.Tests.Architecture;

public class ApiLayerTests
{
    private static readonly Assembly ApiAssembly = typeof(Program).Assembly;
    
    [Fact]
    public void Controllers_Should_Inherit_ControllerBase()
    {
        var result = Types.InAssembly(ApiAssembly)
            .That().HaveNameEndingWith("Controller")
            .Should()
            .Inherit(typeof(ControllerBase))
            .GetResult();
        result.IsSuccessful.Should().BeTrue(
            because: "All controllers must inherit ControllerBase");
    }
    
    [Fact]
    public void Controllers_Should_Have_ApiController_Attribute()
    {
        var result = Types.InAssembly(ApiAssembly)
            .That().HaveNameEndingWith("Controller")
            .Should()
            .HaveCustomAttribute(typeof(ApiControllerAttribute))
            .GetResult();
        result.IsSuccessful.Should().BeTrue();
    }
    
    [Fact]
    public void Controllers_Should_Be_In_Controllers_Namespace()
    {
        var result = Types.InAssembly(ApiAssembly)
            .That().HaveNameEndingWith("Controller")
            .Should()
            .ResideInNamespace("ArchTests.Api.Controllers")
            .GetResult();
        result.IsSuccessful.Should().BeTrue();
    }
}
