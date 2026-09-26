using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RealtimeTaskBoard.Api.Features.Activities;

public class ActivityConfiguration : IEntityTypeConfiguration<ActivityEntity>
{
    public void Configure(EntityTypeBuilder<ActivityEntity> builder)
    {
        builder.ToTable("activities");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.ActorName).HasMaxLength(200).IsRequired();
        builder.Property(a => a.ActionType).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.EntityType).HasMaxLength(50).IsRequired();
        builder.Property(a => a.OldValue).HasMaxLength(2000);
        builder.Property(a => a.NewValue).HasMaxLength(2000);

        builder.HasOne(a => a.Board)
            .WithMany(b => b.Activities)
            .HasForeignKey(a => a.BoardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => new { a.BoardId, a.Timestamp });
    }
}
