using ArchTests.Domain.Entities;
using ArchTests.Infrastructure.Persistence;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;
using System.Reflection;

namespace ArchTests.Tests.Architecture;

public class DomainLayerTests
{
    private static readonly Assembly DomainAssembly = typeof(Product).Assembly;
    
    [Fact]
    public void Domain_Should_Not_DependOn_Infrastructure()
    {
        var result = Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOn("ArchTests.Infrastructure")
            .GetResult();
        result.IsSuccessful.Should().BeTrue(
            because: "Domain layer must not depend on Infrastructure");
    }
    
    [Fact]
    public void Domain_Should_Not_DependOn_Application()
    {
        var result = Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOn("ArchTests.Application")
            .GetResult();
        result.IsSuccessful.Should().BeTrue();
    }
    
    [Fact]
    public void Domain_Entities_Should_Be_In_Correct_Namespace()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That().ResideInNamespace("ArchTests.Domain.Entities")
            .Should()
            .BeClasses()
            .GetResult();
        result.IsSuccessful.Should().BeTrue();
    }
}
