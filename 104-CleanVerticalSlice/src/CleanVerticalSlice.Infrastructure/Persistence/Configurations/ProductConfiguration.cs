namespace CleanVerticalSlice.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CleanVerticalSlice.Domain.Products;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(2000);
        
        builder.OwnsOne(x => x.Price, priceBuilder => {
            priceBuilder.Property(p => p.Amount).HasColumnName("Price").HasColumnType("decimal(18,2)");
            priceBuilder.Property(p => p.Currency).HasColumnName("Currency").HasMaxLength(3);
        });
    }
}
