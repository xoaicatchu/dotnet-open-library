using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using MultiTenant.Api.Core.Entities;
using MultiTenant.Api.Core.Interfaces;

namespace MultiTenant.Api.Infrastructure.Context;

/// <summary>
/// DbContext ứng dụng hỗ trợ Multi-Tenancy:
/// 1. Tự động gắn Global Query Filter dựa trên Tenant hiện tại.
/// 2. Tự động gán TenantId khi thêm thực thể mới.
/// 3. Ngăn chặn sửa đổi TenantId của dữ liệu đã có.
/// </summary>
public class ApplicationDbContext : DbContext
{
    private readonly ITenantContext? _tenantContext;

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantContext? tenantContext = null)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Áp dụng cấu hình và Global Query Filter tự động cho mọi thực thể IMustHaveTenant
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(IMustHaveTenant).IsAssignableFrom(entityType.ClrType))
            {
                // 1. Tạo chỉ mục (Index) trên TenantId để tối ưu hóa hiệu năng câu query có filter
                modelBuilder.Entity(entityType.ClrType)
                    .HasIndex(nameof(IMustHaveTenant.TenantId));

                // 2. Cấu hình Global Query Filter: e => e.TenantId == CurrentTenantId
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(IMustHaveTenant.TenantId));
                
                // Trỏ tới property TenantId của DbContext (được đánh giá động mỗi khi query thực thi)
                var currentTenantIdProperty = Expression.Property(
                    Expression.Constant(this),
                    nameof(CurrentTenantId));

                var filter = Expression.Lambda(
                    Expression.Equal(property, currentTenantIdProperty),
                    parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
            }
        }

        // Cấu hình bảng Product
        modelBuilder.Entity<Product>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.Name).IsRequired().HasMaxLength(200);
            b.Property(p => p.Sku).IsRequired().HasMaxLength(50);
            b.Property(p => p.Price).HasPrecision(18, 2);
        });

        // Cấu hình bảng Order
        modelBuilder.Entity<Order>(b =>
        {
            b.HasKey(o => o.Id);
            b.Property(o => o.CustomerName).IsRequired().HasMaxLength(200);
            b.Property(o => o.TotalAmount).HasPrecision(18, 2);
        });
    }

    /// <summary>
    /// Property cung cấp TenantId hiện tại cho Expression Tree của Global Query Filter.
    /// </summary>
    public string? CurrentTenantId => _tenantContext?.TenantId;

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        EnforceTenantIntegrity();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        EnforceTenantIntegrity();
        return base.SaveChanges();
    }

    /// <summary>
    /// Kiểm soát tính toàn vẹn dữ liệu đa người thuê:
    /// - Tự động điền TenantId khi Add.
    /// - Ngăn chặn giả mạo TenantId khác.
    /// - Ngăn chặn chuyển đổi TenantId khi Update.
    /// </summary>
    private void EnforceTenantIntegrity()
    {
        var currentTenantId = _tenantContext?.TenantId;

        foreach (var entry in ChangeTracker.Entries<IMustHaveTenant>())
        {
            if (entry.State == EntityState.Added)
            {
                if (string.IsNullOrEmpty(entry.Entity.TenantId))
                {
                    if (string.IsNullOrEmpty(currentTenantId))
                    {
                        throw new InvalidOperationException(
                            "Không thể lưu thực thể IMustHaveTenant khi không xác định được Tenant hiện tại trong Context.");
                    }
                    entry.Entity.TenantId = currentTenantId;
                }
                else if (!string.IsNullOrEmpty(currentTenantId) && entry.Entity.TenantId != currentTenantId)
                {
                    // Phát hiện hành vi cố tình inject dữ liệu của Tenant khác
                    throw new UnauthorizedAccessException(
                        $"Vi phạm bảo mật: Tenant '{currentTenantId}' không thể tạo dữ liệu mang TenantId '{entry.Entity.TenantId}'.");
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                // Ngăn chặn đổi quyền sở hữu TenantId
                var originalTenantId = entry.OriginalValues.GetValue<string>(nameof(IMustHaveTenant.TenantId));
                if (entry.Entity.TenantId != originalTenantId)
                {
                    throw new InvalidOperationException("Không được phép thay đổi TenantId của dữ liệu đã tồn tại.");
                }
            }
        }
    }
}
