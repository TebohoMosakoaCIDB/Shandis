
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shandis.Domain.Entities;
using Shandis.Infrastructure.Identity;

namespace Shandis.Infrastructure.Persistence.Configurations;

public class UserBlockConfiguration : IEntityTypeConfiguration<UserBlock>
{
    public void Configure(EntityTypeBuilder<UserBlock> builder)
    {
        builder.ToTable("UserBlocks", table =>
        {
            table.HasCheckConstraint(
                "CK_UserBlocks_NoSelfBlock",
                "\"BlockerId\" <> \"BlockedUserId\"");
        });

        builder.HasKey(x => x.Id);

        builder.HasOne<ShandisUser>()
            .WithMany()
            .HasForeignKey(x => x.BlockerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ShandisUser>()
            .WithMany()
            .HasForeignKey(x => x.BlockedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.BlockerId, x.BlockedUserId })
            .IsUnique();

        builder.HasIndex(x => x.BlockedUserId);
    }
}
