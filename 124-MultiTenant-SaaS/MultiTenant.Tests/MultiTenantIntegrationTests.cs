using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using MultiTenant.Api.Controllers;
using MultiTenant.Api.Core.Entities;

namespace MultiTenant.Tests;

public class MultiTenantIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public MultiTenantIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private HttpClient CreateTenantClient(string? tenantId = null)
    {
        var client = _factory.CreateClient();
        if (!string.IsNullOrEmpty(tenantId))
        {
            client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId);
        }
        return client;
    }

    [Fact]
    public async Task GetTenants_ShouldReturn_SeededTenants_WithoutTenantHeader()
    {
        // Arrange
        var client = CreateTenantClient(); // Không cần header X-Tenant-Id

        // Act
        var response = await client.GetAsync("/api/tenants");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tenants = await response.Content.ReadFromJsonAsync<List<Tenant>>();
        Assert.NotNull(tenants);
        Assert.True(tenants.Count >= 3);
        Assert.Contains(tenants, t => t.Id == "tenant-standard-1");
        Assert.Contains(tenants, t => t.Id == "tenant-enterprise-vip");
    }

    [Fact]
    public async Task ProductsApi_ShouldStrictlyIsolate_DataBetweenTenants()
    {
        // 1. Tạo sản phẩm cho Tenant Standard 1
        var clientA = CreateTenantClient("tenant-standard-1");
        var createResponseA = await clientA.PostAsJsonAsync("/api/products", new CreateProductRequest(
            Name: "ERP Cloud License",
            Sku: "ERP-01",
            Price: 999.00m,
            StockQuantity: 100
        ));
        Assert.Equal(HttpStatusCode.Created, createResponseA.StatusCode);
        var productA = await createResponseA.Content.ReadFromJsonAsync<Product>();
        Assert.NotNull(productA);
        Assert.Equal("tenant-standard-1", productA.TenantId);

        // 2. Tạo sản phẩm cho Tenant Standard 2
        var clientB = CreateTenantClient("tenant-standard-2");
        var createResponseB = await clientB.PostAsJsonAsync("/api/products", new CreateProductRequest(
            Name: "CRM Enterprise Suite",
            Sku: "CRM-01",
            Price: 499.00m,
            StockQuantity: 50
        ));
        Assert.Equal(HttpStatusCode.Created, createResponseB.StatusCode);
        var productB = await createResponseB.Content.ReadFromJsonAsync<Product>();
        Assert.NotNull(productB);
        Assert.Equal("tenant-standard-2", productB.TenantId);

        // 3. Query danh sách sản phẩm bằng Client A: Chỉ được thấy sản phẩm A
        var listResponseA = await clientA.GetAsync("/api/products");
        Assert.Equal(HttpStatusCode.OK, listResponseA.StatusCode);
        var contentA = await listResponseA.Content.ReadAsStringAsync();
        Assert.Contains("ERP Cloud License", contentA);
        Assert.DoesNotContain("CRM Enterprise Suite", contentA); // 0% rò rỉ dữ liệu của Tenant B

        // 4. Query danh sách sản phẩm bằng Client B: Chỉ được thấy sản phẩm B
        var listResponseB = await clientB.GetAsync("/api/products");
        Assert.Equal(HttpStatusCode.OK, listResponseB.StatusCode);
        var contentB = await listResponseB.Content.ReadAsStringAsync();
        Assert.Contains("CRM Enterprise Suite", contentB);
        Assert.DoesNotContain("ERP Cloud License", contentB); // 0% rò rỉ dữ liệu của Tenant A

        // 5. Thử truy cập trực tiếp ID của Tenant A bằng Client B -> Phải trả về 404 Not Found!
        var directAccessByB = await clientB.GetAsync($"/api/products/{productA.Id}");
        Assert.Equal(HttpStatusCode.NotFound, directAccessByB.StatusCode);
    }

    [Fact]
    public async Task DedicatedDbTenant_ShouldOperate_Independently()
    {
        // VIP Tenant sở hữu Dedicated DB riêng
        var vipClient = CreateTenantClient("tenant-enterprise-vip");

        // Tạo sản phẩm cho VIP
        var createResponse = await vipClient.PostAsJsonAsync("/api/products", new CreateProductRequest(
            Name: "Custom AI Hardware Server",
            Sku: "AI-SRV-01",
            Price: 15000.00m,
            StockQuantity: 5
        ));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        // Query lại
        var getResponse = await vipClient.GetAsync("/api/products");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var content = await getResponse.Content.ReadAsStringAsync();
        Assert.Contains("Dedicated Database (Physical Isolation)", content);
        Assert.Contains("Custom AI Hardware Server", content);

        // Đảm bảo Tenant Standard 1 không hề nhìn thấy sản phẩm này
        var standardClient = CreateTenantClient("tenant-standard-1");
        var standardContent = await (await standardClient.GetAsync("/api/products")).Content.ReadAsStringAsync();
        Assert.DoesNotContain("Custom AI Hardware Server", standardContent);
    }

    [Fact]
    public async Task OnboardNewTenant_ShouldImmediately_SupportRequests()
    {
        var client = CreateTenantClient();

        // 1. Onboarding tenant mới
        var newTenantId = $"startup-{Guid.NewGuid():N}"[..12];
        var onboardResponse = await client.PostAsJsonAsync("/api/tenants", new CreateTenantRequest(
            Id: newTenantId,
            Name: "Fast Startup Inc",
            Subdomain: newTenantId,
            Tier: TenantTier.Standard
        ));
        Assert.Equal(HttpStatusCode.Created, onboardResponse.StatusCode);

        // 2. Ngay lập tức dùng Header của tenant mới để tạo sản phẩm
        var newTenantClient = CreateTenantClient(newTenantId);
        var createProductResponse = await newTenantClient.PostAsJsonAsync("/api/products", new CreateProductRequest(
            Name: "SaaS Starter Plan",
            Sku: "STARTER-01",
            Price: 29.00m,
            StockQuantity: 999
        ));
        Assert.Equal(HttpStatusCode.Created, createProductResponse.StatusCode);

        // 3. Query xác nhận
        var getProductsResponse = await newTenantClient.GetAsync("/api/products");
        Assert.Equal(HttpStatusCode.OK, getProductsResponse.StatusCode);
        var content = await getProductsResponse.Content.ReadAsStringAsync();
        Assert.Contains("SaaS Starter Plan", content);
    }
}
