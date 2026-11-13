
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shandis.Domain.Entities;
using Shandis.Infrastructure.Identity;

namespace Shandis.Infrastructure.Persistence.Configurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("Conversations", table =>
        {
            table.HasCheckConstraint(
                "CK_Conversations_DifferentUsers",
                "\"UserOneId\" <> \"UserTwoId\"");
        });

        builder.HasKey(x => x.Id);

        builder.HasOne<ShandisUser>()
            .WithMany()
            .HasForeignKey(x => x.UserOneId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ShandisUser>()
            .WithMany()
            .HasForeignKey(x => x.UserTwoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Messages)
            .WithOne(x => x.Conversation)
            .HasForeignKey(x => x.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        // One conversation for each unique pair of users.
        builder.HasIndex(x => new { x.UserOneId, x.UserTwoId })
            .IsUnique();

        builder.HasIndex(x => x.LastMessageAtUtc);
    }
}
