using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shandis.Domain.Entities;
using Shandis.Infrastructure.Identity;

namespace Shandis.Infrastructure.Persistence.Configurations;

public class MomentCommentConfiguration : IEntityTypeConfiguration<MomentComment>
{
    public void Configure(EntityTypeBuilder<MomentComment> builder)
    {
        builder.ToTable("MomentComments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Content)
            .HasMaxLength(MomentComment.MaxContentLength)
            .IsRequired();

        builder.HasOne(x => x.Moment)
            .WithMany(x => x.Comments)
            .HasForeignKey(x => x.MomentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ParentComment)
            .WithMany(x => x.Replies)
            .HasForeignKey(x => x.ParentCommentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ShandisUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.MomentId, x.ParentCommentId, x.CreatedAtUtc });
        builder.HasIndex(x => x.ParentCommentId);
        builder.HasIndex(x => x.UserId);
    }
}
