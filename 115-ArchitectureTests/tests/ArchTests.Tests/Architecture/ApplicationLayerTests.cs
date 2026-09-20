using ArchTests.Application.Services;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;
using System.Reflection;

namespace ArchTests.Tests.Architecture;

public class ApplicationLayerTests
{
    private static readonly Assembly AppAssembly = typeof(ProductService).Assembly;
    
    [Fact]
    public void Application_Should_Not_DependOn_Infrastructure()
    {
        var result = Types.InAssembly(AppAssembly)
            .Should()
            .NotHaveDependencyOn("ArchTests.Infrastructure")
            .GetResult();
        result.IsSuccessful.Should().BeTrue(
            because: "Application layer must not know about Infrastructure");
    }
    
    [Fact]
    public void Services_Should_Be_In_Services_Namespace()
    {
        var result = Types.InAssembly(AppAssembly)
            .That().HaveNameEndingWith("Service")
            .Should()
            .ResideInNamespace("ArchTests.Application.Services")
            .GetResult();
        result.IsSuccessful.Should().BeTrue();
    }
}
