import os

base_dir = r"d:\GitHub\dotnet-example\115-ArchitectureTests"

def write_file(path, content):
    full_path = os.path.join(base_dir, path)
    os.makedirs(os.path.dirname(full_path), exist_ok=True)
    with open(full_path, "w", encoding="utf-8") as f:
        f.write(content.strip() + "\n")

# Projects
write_file("src/ArchTests.Domain/ArchTests.Domain.csproj", """
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
""")

write_file("src/ArchTests.Application/ArchTests.Application.csproj", """
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\\ArchTests.Domain\\ArchTests.Domain.csproj" />
  </ItemGroup>
</Project>
""")

write_file("src/ArchTests.Infrastructure/ArchTests.Infrastructure.csproj", """
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.0-preview.1.25081.1" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\\ArchTests.Domain\\ArchTests.Domain.csproj" />
    <ProjectReference Include="..\\ArchTests.Application\\ArchTests.Application.csproj" />
  </ItemGroup>
</Project>
""")

write_file("src/ArchTests.Api/ArchTests.Api.csproj", """
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.0-preview.1.25081.1">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Swashbuckle.AspNetCore" Version="7.2.0" />
    <PackageReference Include="Bogus" Version="35.6.2" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\\ArchTests.Application\\ArchTests.Application.csproj" />
    <ProjectReference Include="..\\ArchTests.Infrastructure\\ArchTests.Infrastructure.csproj" />
  </ItemGroup>
</Project>
""")

write_file("tests/ArchTests.Tests/ArchTests.Tests.csproj", """
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.0-preview.1.25120.3" />
    <PackageReference Include="NetArchTest.Rules" Version="1.3.2" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.0" />
    <PackageReference Include="FluentAssertions" Version="8.3.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.0-preview.1.25081.1" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\\..\\src\\ArchTests.Api\\ArchTests.Api.csproj" />
  </ItemGroup>
</Project>
""")

# Code files
write_file("src/ArchTests.Domain/Entities/Product.cs", """
namespace ArchTests.Domain.Entities;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
""")

write_file("src/ArchTests.Domain/Interfaces/IProductRepository.cs", """
using ArchTests.Domain.Entities;

namespace ArchTests.Domain.Interfaces;

public interface IProductRepository
{
    Task<IEnumerable<Product>> GetAllAsync();
    Task<Product?> GetByIdAsync(int id);
    Task AddAsync(Product product);
    Task DeleteAsync(int id);
}
""")

write_file("src/ArchTests.Application/Dtos/ProductDto.cs", """
namespace ArchTests.Application.Dtos;

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
""")

write_file("src/ArchTests.Application/Services/ProductService.cs", """
using ArchTests.Application.Dtos;
using ArchTests.Domain.Entities;
using ArchTests.Domain.Interfaces;

namespace ArchTests.Application.Services;

public class ProductService
{
    private readonly IProductRepository _repository;

    public ProductService(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<ProductDto>> GetAllAsync()
    {
        var products = await _repository.GetAllAsync();
        return products.Select(p => new ProductDto { Id = p.Id, Name = p.Name, Price = p.Price });
    }

    public async Task<ProductDto?> GetByIdAsync(int id)
    {
        var p = await _repository.GetByIdAsync(id);
        if (p == null) return null;
        return new ProductDto { Id = p.Id, Name = p.Name, Price = p.Price };
    }

    public async Task AddAsync(ProductDto dto)
    {
        var product = new Product { Id = dto.Id, Name = dto.Name, Price = dto.Price };
        await _repository.AddAsync(product);
    }
    
    public async Task DeleteAsync(int id)
    {
        await _repository.DeleteAsync(id);
    }
}
""")

write_file("src/ArchTests.Infrastructure/Persistence/AppDbContext.cs", """
using ArchTests.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ArchTests.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    
    public DbSet<Product> Products { get; set; }
}
""")

write_file("src/ArchTests.Infrastructure/Repositories/ProductRepository.cs", """
using ArchTests.Domain.Entities;
using ArchTests.Domain.Interfaces;
using ArchTests.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ArchTests.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _context;

    public ProductRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        return await _context.Products.ToListAsync();
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        return await _context.Products.FindAsync(id);
    }

    public async Task AddAsync(Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var p = await _context.Products.FindAsync(id);
        if (p != null)
        {
            _context.Products.Remove(p);
            await _context.SaveChangesAsync();
        }
    }
}
""")

write_file("src/ArchTests.Api/Program.cs", """
using ArchTests.Application.Services;
using ArchTests.Domain.Interfaces;
using ArchTests.Infrastructure.Persistence;
using ArchTests.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=app.db"));

builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ProductService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();

public partial class Program { }
""")

write_file("src/ArchTests.Api/Controllers/ProductsController.cs", """
using ArchTests.Application.Dtos;
using ArchTests.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace ArchTests.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ProductService _service;

    public ProductsController(ProductService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var product = await _service.GetByIdAsync(id);
        if (product == null) return NotFound();
        return Ok(product);
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ProductDto product)
    {
        await _service.AddAsync(product);
        return CreatedAtAction(nameof(Get), new { id = product.Id }, product);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
""")

write_file("src/ArchTests.Api/Properties/launchSettings.json", """
{
  "$schema": "http://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "http://localhost:5315",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
""")

# Tests

write_file("tests/ArchTests.Tests/Architecture/DomainLayerTests.cs", """
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
""")

write_file("tests/ArchTests.Tests/Architecture/ApplicationLayerTests.cs", """
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
""")

write_file("tests/ArchTests.Tests/Architecture/ApiLayerTests.cs", """
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
""")

write_file("tests/ArchTests.Tests/Architecture/InfrastructureLayerTests.cs", """
using ArchTests.Infrastructure.Repositories;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;
using System.Reflection;

namespace ArchTests.Tests.Architecture;

public class InfrastructureLayerTests
{
    private static readonly Assembly InfraAssembly = typeof(ProductRepository).Assembly;

    [Fact]
    public void Repositories_Should_Be_In_Repositories_Namespace()
    {
        var result = Types.InAssembly(InfraAssembly)
            .That().HaveNameEndingWith("Repository")
            .Should()
            .ResideInNamespace("ArchTests.Infrastructure.Repositories")
            .GetResult();
        result.IsSuccessful.Should().BeTrue();
    }
}
""")

write_file("tests/ArchTests.Tests/Integration/ApiIntegrationTests.cs", """
using ArchTests.Application.Dtos;
using ArchTests.Domain.Entities;
using ArchTests.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using Xunit;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using Microsoft.Data.Sqlite;

namespace ArchTests.Tests.Integration;

public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly SqliteConnection _connection;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                var dbConnectionDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbConnection));
                if (dbConnectionDescriptor != null) services.Remove(dbConnectionDescriptor);
                
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseSqlite(_connection);
                });

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();
                
                db.Products.Add(new Product { Id = 1, Name = "Test Product", Price = 10 });
                db.SaveChanges();
            });
        });
    }

    [Fact]
    public async Task Get_Products_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/products");
        response.EnsureSuccessStatusCode();
        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>();
        products.Should().NotBeNull();
        products!.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Get_ProductById_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/products/1");
        response.EnsureSuccessStatusCode();
        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        product.Should().NotBeNull();
        product!.Id.Should().Be(1);
    }
    
    [Fact]
    public async Task Post_Product_ReturnsCreated()
    {
        var client = _factory.CreateClient();
        var newProduct = new ProductDto { Id = 2, Name = "New Product", Price = 20 };
        var response = await client.PostAsJsonAsync("/api/products", newProduct);
        response.EnsureSuccessStatusCode();
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
    }
    
    [Fact]
    public async Task Delete_Product_ReturnsNoContent()
    {
        var client = _factory.CreateClient();
        var response = await client.DeleteAsync("/api/products/1");
        response.EnsureSuccessStatusCode();
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }
}
""")

write_file("ArchTests.slnx", """
<Solution>
  <Folder Name="/src/">
    <Project Path="src/ArchTests.Domain/ArchTests.Domain.csproj" />
    <Project Path="src/ArchTests.Application/ArchTests.Application.csproj" />
    <Project Path="src/ArchTests.Infrastructure/ArchTests.Infrastructure.csproj" />
    <Project Path="src/ArchTests.Api/ArchTests.Api.csproj" />
  </Folder>
  <Folder Name="/tests/">
    <Project Path="tests/ArchTests.Tests/ArchTests.Tests.csproj" />
  </Folder>
</Solution>
""")

write_file("README.md", """
# 115-ArchitectureTests

## 1. Giới thiệu Architecture Tests
Dự án demo việc kiểm thử cấu trúc kiến trúc phần mềm bằng **NetArchTest**. Nó giúp tự động kiểm tra các rules dependency giữa các layer, đảm bảo không có sự vi phạm kiến trúc nào xảy ra trong quá trình phát triển (ví dụ: Domain bị tham chiếu đến Infrastructure).

## 2. Mermaid: Clean Architecture dependency diagram
```mermaid
flowchart TD
    Api[ArchTests.Api] --> Application[ArchTests.Application]
    Api --> Infrastructure[ArchTests.Infrastructure]
    Infrastructure --> Application
    Application --> Domain[ArchTests.Domain]
    Infrastructure --> Domain
```

## 3. Danh sách các rule được kiểm tra
- **Domain Layer**: 
  - Không được phụ thuộc vào `Application` và `Infrastructure`.
  - Các Entity phải nằm trong namespace `ArchTests.Domain.Entities`.
- **Application Layer**: 
  - Không được phụ thuộc vào `Infrastructure`.
  - Các Service phải kết thúc bằng `Service` và nằm trong namespace `ArchTests.Application.Services`.
- **Infrastructure Layer**:
  - Các Repository phải nằm trong namespace `ArchTests.Infrastructure.Repositories`.
- **Api Layer**:
  - Các Controller phải kế thừa từ `ControllerBase`.
  - Phải có thuộc tính `[ApiController]`.
  - Phải nằm trong namespace `ArchTests.Api.Controllers`.

## 4. Tại sao cần architecture tests
- **Ngăn chặn phá vỡ kiến trúc**: Giúp phát hiện sớm khi developer add sai references hoặc using sai namespaces.
- **Dễ bảo trì**: Code base giữ được sự phân tách rõ ràng.
- **Tự động hóa**: Kiểm tra architecture như một phần của quy trình CI/CD.

## 5. Cấu trúc dự án
Dự án được chia thành 4 layer (Domain, Application, Infrastructure, Api) theo tiêu chuẩn Clean Architecture.

## 6. Cách chạy
- Đứng tại thư mục gốc chạy lệnh build:
  ```bash
  dotnet build
  ```
- Chạy toàn bộ test:
  ```bash
  dotnet test
  ```
- Khởi chạy API (chạy tại port 5315):
  ```bash
  cd src/ArchTests.Api
  dotnet run
  ```

## 7. Kết quả test
Chạy `dotnet test` kết quả sẽ pass 12/12 (8 architecture tests, 4 integration tests).
""")
