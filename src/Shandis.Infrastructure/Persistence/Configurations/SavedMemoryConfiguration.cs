using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shandis.Domain.Entities;
using Shandis.Infrastructure.Identity;

namespace Shandis.Infrastructure.Persistence.Configurations;

public sealed class SavedMemoryConfiguration : IEntityTypeConfiguration<SavedMemory>
{
    public void Configure(EntityTypeBuilder<SavedMemory> builder)
    {
        builder.ToTable("SavedMemories");
        builder.HasKey(x => x.Id);

        builder.HasOne<ShandisUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Memory)
            .WithMany()
            .HasForeignKey(x => x.MemoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.UserId, x.MemoryId }).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.CreatedAtUtc });
    }
}
