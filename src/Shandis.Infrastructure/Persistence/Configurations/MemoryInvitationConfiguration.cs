using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shandis.Domain.Entities;
using Shandis.Infrastructure.Identity;

namespace Shandis.Infrastructure.Persistence.Configurations;

public class MemoryInvitationConfiguration
    : IEntityTypeConfiguration<MemoryInvitation>
{
    public void Configure(EntityTypeBuilder<MemoryInvitation> builder)
    {
        builder.ToTable("MemoryInvitations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.InvitedEmail)
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(x => x.TokenHash)
            .HasMaxLength(256)
            .IsRequired();

        builder.HasOne(x => x.Memory)
            .WithMany(x => x.Invitations)
            .HasForeignKey(x => x.MemoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ShandisUser>()
            .WithMany()
            .HasForeignKey(x => x.InvitedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ShandisUser>()
            .WithMany()
            .HasForeignKey(x => x.InvitedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => new { x.MemoryId, x.Status });
        builder.HasIndex(x => x.InvitedEmail);
    }
}