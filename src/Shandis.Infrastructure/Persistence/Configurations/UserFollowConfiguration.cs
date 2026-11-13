
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shandis.Domain.Entities;
using Shandis.Infrastructure.Identity;

namespace Shandis.Infrastructure.Persistence.Configurations;

public class UserFollowConfiguration : IEntityTypeConfiguration<UserFollow>
{
    public void Configure(EntityTypeBuilder<UserFollow> builder)
    {
        builder.ToTable("UserFollows", table =>
        {
            table.HasCheckConstraint(
                "CK_UserFollows_NoSelfFollow",
                "\"FollowerId\" <> \"FollowedUserId\"");
        });

        builder.HasKey(x => x.Id);

        builder.HasOne<ShandisUser>()
            .WithMany()
            .HasForeignKey(x => x.FollowerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ShandisUser>()
            .WithMany()
            .HasForeignKey(x => x.FollowedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Prevent duplicate follow relationships.
        builder.HasIndex(x => new { x.FollowerId, x.FollowedUserId })
            .IsUnique();

        // Efficient follower/following lookups.
        builder.HasIndex(x => x.FollowedUserId);
    }
}
