using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shandis.Domain.Entities;
using Shandis.Infrastructure.Identity;

namespace Shandis.Infrastructure.Persistence.Configurations;

public class MomentReactionConfiguration
    : IEntityTypeConfiguration<MomentReaction>
{
    public void Configure(EntityTypeBuilder<MomentReaction> builder)
    {
        builder.ToTable("MomentReactions");

        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Moment)
            .WithMany(x => x.Reactions)
            .HasForeignKey(x => x.MomentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ShandisUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // One reaction per user per Moment.
        builder.HasIndex(x => new { x.MomentId, x.UserId })
            .IsUnique();
    }
}