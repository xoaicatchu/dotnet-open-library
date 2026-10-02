using Microsoft.EntityFrameworkCore;
using MultiTenant.Api.Core.Interfaces;
using MultiTenant.Api.Infrastructure.Context;
using MultiTenant.Api.Infrastructure.Middleware;
using MultiTenant.Api.Infrastructure.Services;
using MultiTenant.Api.Infrastructure.Strategies;

var builder = WebApplication.CreateBuilder(args);

// 1. Thêm Controllers
builder.Services.AddControllers();

// 2. Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 3. Đăng ký các dịch vụ Multi-Tenancy
// 3.1. Tenant Catalog Store (Lưu trữ metadata các tenant)
builder.Services.AddSingleton<ITenantStore, InMemoryTenantStore>();

// 3.2. Các chiến lược trích xuất Tenant (Thứ tự ưu tiên: Header -> Subdomain)
builder.Services.AddSingleton<ITenantResolutionStrategy, HeaderTenantResolutionStrategy>();
builder.Services.AddSingleton<ITenantResolutionStrategy, SubdomainTenantResolutionStrategy>();

// 3.3. Tenant Context (Scoped cho mỗi HTTP Request)
builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());

// 3.4. Dynamic ApplicationDbContext Factory (Hybrid Multi-Tenancy Routing)
builder.Services.AddScoped<ApplicationDbContext>(sp =>
{
    var tenantContext = sp.GetRequiredService<ITenantContext>();
    var configuration = sp.GetRequiredService<IConfiguration>();

    var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

    // Nếu là Enterprise Tenant sở hữu Dedicated DB:
    if (!string.IsNullOrEmpty(tenantContext.CurrentTenant?.DedicatedConnectionString))
    {
        optionsBuilder.UseSqlite(tenantContext.CurrentTenant.DedicatedConnectionString);
    }
    else
    {
        // Khách hàng Standard dùng Shared Database với Global Query Filter
        var defaultConn = configuration.GetConnectionString("DefaultConnection") 
            ?? "Data Source=shared_saas.db";
        optionsBuilder.UseSqlite(defaultConn);
    }

    return new ApplicationDbContext(optionsBuilder.Options, tenantContext);
});

var app = builder.Build();

// 4. Khởi tạo cấu trúc Database khi startup
using (var scope = app.Services.CreateScope())
{
    var tenantStore = scope.ServiceProvider.GetRequiredService<ITenantStore>();
    var tenants = await tenantStore.GetAllTenantsAsync();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

    // 4.1. Khởi tạo Shared Database
    var sharedOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseSqlite(config.GetConnectionString("DefaultConnection") ?? "Data Source=shared_saas.db")
        .Options;
    using (var sharedDb = new ApplicationDbContext(sharedOptions))
    {
        await sharedDb.Database.EnsureCreatedAsync();
    }

    // 4.2. Khởi tạo Dedicated Database cho các Enterprise Tenant
    foreach (var t in tenants.Where(t => !string.IsNullOrEmpty(t.DedicatedConnectionString)))
    {
        var dedicatedOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(t.DedicatedConnectionString!)
            .Options;
        using (var dedicatedDb = new ApplicationDbContext(dedicatedOptions))
        {
            await dedicatedDb.Database.EnsureCreatedAsync();
        }
    }
}

// 5. Pipeline Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Kích hoạt Tenant Resolution Middleware trước khi vào Controllers
app.UseMiddleware<TenantResolutionMiddleware>();

app.MapControllers();

app.Run();

// Hỗ trợ WebApplicationFactory trong Integration Tests
public partial class Program { }
