using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shandis.Domain.Entities;
using Shandis.Infrastructure.Identity;

namespace Shandis.Infrastructure.Persistence.Configurations;

public class MomentMediaConfiguration : IEntityTypeConfiguration<MomentMedia>
{
    public void Configure(EntityTypeBuilder<MomentMedia> builder)
    {
        builder.ToTable("MomentMedia");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.BlobName)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.ContentType)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.FileSizeBytes)
            .IsRequired();

        builder.HasOne(x => x.Moment)
            .WithMany(x => x.Media)
            .HasForeignKey(x => x.MomentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ShandisUser>()
            .WithMany()
            .HasForeignKey(x => x.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.MomentId, x.SortOrder });
        builder.HasIndex(x => x.BlobName).IsUnique();
    }
}