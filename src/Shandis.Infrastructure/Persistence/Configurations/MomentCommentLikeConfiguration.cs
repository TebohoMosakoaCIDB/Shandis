using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shandis.Domain.Entities;
using Shandis.Infrastructure.Identity;

namespace Shandis.Infrastructure.Persistence.Configurations;

public class MomentCommentLikeConfiguration : IEntityTypeConfiguration<MomentCommentLike>
{
    public void Configure(EntityTypeBuilder<MomentCommentLike> builder)
    {
        builder.ToTable("MomentCommentLikes");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Comment)
            .WithMany(x => x.Likes)
            .HasForeignKey(x => x.CommentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ShandisUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.CommentId, x.UserId }).IsUnique();
        builder.HasIndex(x => x.UserId);
    }
}
