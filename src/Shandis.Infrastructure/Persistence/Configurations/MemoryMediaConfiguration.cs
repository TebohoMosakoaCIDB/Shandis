using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shandis.Domain.Entities;
using Shandis.Infrastructure.Identity;

namespace Shandis.Infrastructure.Persistence.Configurations;

public sealed class MemoryMediaConfiguration : IEntityTypeConfiguration<MemoryMedia>
{
    public void Configure(EntityTypeBuilder<MemoryMedia> builder)
    {
        builder.ToTable("MemoryMedia");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.BlobName).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(100).IsRequired();

        builder.HasOne(x => x.Memory)
            .WithMany()
            .HasForeignKey(x => x.MemoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ShandisUser>()
            .WithMany()
            .HasForeignKey(x => x.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.MemoryId);
        builder.HasIndex(x => x.UploadedByUserId);
    }
}
