using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shandis.Domain.Entities;
using Shandis.Infrastructure.Identity;

namespace Shandis.Infrastructure.Persistence.Configurations;

public class MemoryCommentConfiguration : IEntityTypeConfiguration<MemoryComment>
{
    public void Configure(EntityTypeBuilder<MemoryComment> builder)
    {
        builder.ToTable("MemoryComments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Content)
            .HasMaxLength(2000)
            .IsRequired();

        builder.HasOne(x => x.Memory)
            .WithMany(x => x.Comments)
            .HasForeignKey(x => x.MemoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ShandisUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.MemoryId, x.CreatedAtUtc });
        builder.HasIndex(x => x.UserId);
    }
}