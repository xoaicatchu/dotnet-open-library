using Microsoft.AspNetCore.Http;
using MultiTenant.Api.Core.Entities;
using MultiTenant.Api.Core.Interfaces;
using MultiTenant.Api.Infrastructure.Context;
using MultiTenant.Api.Infrastructure.Middleware;
using MultiTenant.Api.Infrastructure.Services;
using MultiTenant.Api.Infrastructure.Strategies;

namespace MultiTenant.Tests;

public class TenantResolutionTests
{
    private readonly ITenantStore _tenantStore = new InMemoryTenantStore();

    [Fact]
    public async Task HeaderStrategy_ShouldResolve_TenantId_From_X_Tenant_Id()
    {
        // Arrange
        var strategy = new HeaderTenantResolutionStrategy();
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Tenant-Id"] = "tenant-standard-1";

        // Act
        var result = await strategy.ResolveTenantIdentifierAsync(context);

        // Assert
        Assert.Equal("tenant-standard-1", result);
    }

    [Fact]
    public async Task SubdomainStrategy_ShouldResolve_Subdomain_From_Host()
    {
        // Arrange
        var strategy = new SubdomainTenantResolutionStrategy();
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("acme.saas.local:5000");

        // Act
        var result = await strategy.ResolveTenantIdentifierAsync(context);

        // Assert
        Assert.Equal("acme", result);
    }

    [Fact]
    public async Task Middleware_ShouldReturn400_WhenTenantIdentifier_IsMissing()
    {
        // Arrange
        var strategies = new ITenantResolutionStrategy[] { new HeaderTenantResolutionStrategy() };
        var middleware = new TenantResolutionMiddleware(_ => Task.CompletedTask, strategies);

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/products"; // Non-exempt path

        var tenantContext = new TenantContext();

        // Act
        await middleware.InvokeAsync(context, _tenantStore, tenantContext);

        // Assert
        Assert.Equal(400, context.Response.StatusCode);
        Assert.False(tenantContext.HasTenant);
    }

    [Fact]
    public async Task Middleware_ShouldReturn404_WhenTenant_DoesNotExist()
    {
        // Arrange
        var strategies = new ITenantResolutionStrategy[] { new HeaderTenantResolutionStrategy() };
        var middleware = new TenantResolutionMiddleware(_ => Task.CompletedTask, strategies);

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/products";
        context.Request.Headers["X-Tenant-Id"] = "non-existent-tenant";

        var tenantContext = new TenantContext();

        // Act
        await middleware.InvokeAsync(context, _tenantStore, tenantContext);

        // Assert
        Assert.Equal(404, context.Response.StatusCode);
        Assert.False(tenantContext.HasTenant);
    }

    [Fact]
    public async Task Middleware_ShouldReturn403_WhenTenant_IsSuspended()
    {
        // Arrange
        var strategies = new ITenantResolutionStrategy[] { new HeaderTenantResolutionStrategy() };
        var middleware = new TenantResolutionMiddleware(_ => Task.CompletedTask, strategies);

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/products";
        context.Request.Headers["X-Tenant-Id"] = "tenant-suspended";

        var tenantContext = new TenantContext();

        // Act
        await middleware.InvokeAsync(context, _tenantStore, tenantContext);

        // Assert
        Assert.Equal(403, context.Response.StatusCode);
        Assert.False(tenantContext.HasTenant);
    }

    [Fact]
    public async Task Middleware_ShouldAllowExemptPaths_WithoutTenantHeader()
    {
        // Arrange
        var executed = false;
        var strategies = new ITenantResolutionStrategy[] { new HeaderTenantResolutionStrategy() };
        var middleware = new TenantResolutionMiddleware(_ =>
        {
            executed = true;
            return Task.CompletedTask;
        }, strategies);

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/tenants"; // Exempt path

        var tenantContext = new TenantContext();

        // Act
        await middleware.InvokeAsync(context, _tenantStore, tenantContext);

        // Assert
        Assert.True(executed);
        Assert.Equal(200, context.Response.StatusCode);
    }
}
