using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shandis.Domain.Entities;
using Shandis.Infrastructure.Identity;

namespace Shandis.Infrastructure.Persistence.Configurations;

public class MemoryParticipantConfiguration
    : IEntityTypeConfiguration<MemoryParticipant>
{
    public void Configure(EntityTypeBuilder<MemoryParticipant> builder)
    {
        builder.ToTable("MemoryParticipants");

        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Memory)
            .WithMany(x => x.Participants)
            .HasForeignKey(x => x.MemoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ShandisUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // A user has one participant record per Memory.
        builder.HasIndex(x => new { x.MemoryId, x.UserId })
            .IsUnique();

        builder.HasIndex(x => new { x.UserId, x.Status });
    }
}