using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shandis.Domain.Entities;
using Shandis.Infrastructure.Identity;

namespace Shandis.Infrastructure.Persistence.Configurations;

public class MemoryMomentConfiguration : IEntityTypeConfiguration<MemoryMoment>
{
    public void Configure(EntityTypeBuilder<MemoryMoment> builder)
    {
        builder.ToTable("MemoryMoments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .HasMaxLength(200);

        builder.Property(x => x.Content)
            .HasColumnType("text");
        builder.HasOne(x => x.Memory)
            .WithMany(x => x.Moments)
            .HasForeignKey(x => x.MemoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ShandisUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Media)
            .WithOne(x => x.Moment)
            .HasForeignKey(x => x.MomentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Reactions)
            .WithOne(x => x.Moment)
            .HasForeignKey(x => x.MomentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.MemoryId, x.Status });
        builder.HasIndex(x => x.CreatedByUserId);
        builder.HasIndex(x => x.CreatedAtUtc);
    }
}