using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace StockFlow.Api.Data;
public sealed class StockFlowDbContext(DbContextOptions<StockFlowDbContext> options) : IdentityDbContext<IdentityUser>(options), IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<StockDocument> StockDocuments => Set<StockDocument>();
    public DbSet<StockDocumentLine> StockDocumentLines => Set<StockDocumentLine>();
    public DbSet<StockBalance> StockBalances => Set<StockBalance>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<ClientOperation> ClientOperations => Set<ClientOperation>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        base.OnModelCreating(model);
        model.Entity<Product>(e =>
        {
            e.HasIndex(x => x.Sku).IsUnique(); e.HasIndex(x => x.Barcode).IsUnique();
            e.Property(x => x.Sku).HasMaxLength(64); e.Property(x => x.Barcode).HasMaxLength(128);
            e.Property(x => x.Name).HasMaxLength(200); e.Property(x => x.Unit).HasMaxLength(32);
            e.ToTable(t => t.HasCheckConstraint("CK_Product_Numbers", "\"PurchasePriceVnd\" BETWEEN 0 AND 9007199254740991 AND \"SalePriceVnd\" BETWEEN 0 AND 9007199254740991 AND \"MinimumStock\" BETWEEN 0 AND 9007199254740991"));
        });
        model.Entity<Supplier>(e => { e.HasIndex(x => x.Code).IsUnique(); e.Property(x => x.Code).HasMaxLength(64); e.Property(x => x.Name).HasMaxLength(200); e.Property(x => x.Phone).HasMaxLength(32); });
        model.Entity<StockDocument>(e =>
        {
            e.HasIndex(x => x.Number).IsUnique(); e.Property(x => x.Number).HasMaxLength(64);
            e.Property(x => x.Type).HasConversion<string>(); e.Property(x => x.Status).HasConversion<string>();
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasOne<Supplier>().WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<IdentityUser>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<IdentityUser>().WithMany().HasForeignKey(x => x.PostedBy).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<IdentityUser>().WithMany().HasForeignKey(x => x.CancelledBy).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
            e.ToTable(t => { t.HasCheckConstraint("CK_Document_Version", "\"Version\" BETWEEN 1 AND 9007199254740991"); t.HasCheckConstraint("CK_Document_State", "\"Status\" IN ('Draft','Posted','Cancelled') AND \"Type\" IN ('Receipt','Issue','Stocktake')"); });
        });
        model.Entity<StockDocumentLine>(e =>
        {
            e.HasIndex(x => new { x.DocumentId, x.ProductId }).IsUnique();
            e.ToTable(t => t.HasCheckConstraint("CK_Line_ObservedVersion", "\"ObservedBalanceVersion\" IS NULL OR \"ObservedBalanceVersion\" BETWEEN 1 AND 9007199254740991"));
            e.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.ToTable(t => t.HasCheckConstraint("CK_Line_Numbers", "\"Quantity\" BETWEEN 0 AND 9007199254740991 AND (\"UnitCostVnd\" IS NULL OR \"UnitCostVnd\" BETWEEN 0 AND 9007199254740991) AND (\"CountedQuantity\" IS NULL OR \"CountedQuantity\" BETWEEN 0 AND 9007199254740991) AND (\"SnapshotQuantity\" IS NULL OR \"SnapshotQuantity\" BETWEEN 0 AND 9007199254740991) AND (\"SnapshotBalanceVersion\" IS NULL OR \"SnapshotBalanceVersion\" BETWEEN 1 AND 9007199254740991)"));
        });
        model.Entity<StockBalance>(e =>
        {
            e.HasKey(x => x.ProductId); e.Property(x => x.Version).IsConcurrencyToken();
            e.HasOne<Product>().WithOne().HasForeignKey<StockBalance>(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.ToTable(t => t.HasCheckConstraint("CK_Balance_Numbers", "\"Quantity\" BETWEEN 0 AND 9007199254740991 AND \"Version\" BETWEEN 1 AND 9007199254740991"));
        });
        model.Entity<StockMovement>(e =>
        {
            e.Property(x => x.Kind).HasConversion<string>();
            e.HasIndex(x => x.LineId).IsUnique().HasFilter("\"ReversesMovementId\" IS NULL");
            e.HasIndex(x => x.ReversesMovementId).IsUnique(); e.HasIndex(x => new { x.ProductId, x.OccurredAtUtc });
            e.HasOne<StockDocument>().WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<StockDocumentLine>().WithMany().HasForeignKey(x => x.LineId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<IdentityUser>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<StockMovement>().WithMany().HasForeignKey(x => x.ReversesMovementId).OnDelete(DeleteBehavior.Restrict);
            e.ToTable(t => t.HasCheckConstraint("CK_Movement_Delta", "\"Delta\" BETWEEN -9007199254740991 AND 9007199254740991"));
        });
        model.Entity<ClientOperation>(e => { e.HasKey(x => new { x.UserId, x.ClientOperationId }); e.Property(x => x.OperationType).HasMaxLength(64); e.Property(x => x.RequestHashSha256).HasMaxLength(64); e.HasOne<IdentityUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict); });
    }
}
