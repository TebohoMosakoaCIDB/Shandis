
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Shandis.Domain.Entities;
using Shandis.Infrastructure.Identity;

namespace Shandis.Infrastructure.Persistence;

public class ShandisDbContext
    : IdentityDbContext<ShandisUser, IdentityRole<Guid>, Guid>
{
    public ShandisDbContext(DbContextOptions<ShandisDbContext> options)
        : base(options)
    {
    }

    public DbSet<Memory> Memories => Set<Memory>();

    public DbSet<MemoryMoment> MemoryMoments => Set<MemoryMoment>();

    public DbSet<MomentMedia> MomentMedia => Set<MomentMedia>();

    public DbSet<MemoryParticipant> MemoryParticipants => Set<MemoryParticipant>();

    public DbSet<MemoryInvitation> MemoryInvitations => Set<MemoryInvitation>();

    public DbSet<MemoryCategory> MemoryCategories => Set<MemoryCategory>();

    public DbSet<Place> Places => Set<Place>();

    public DbSet<MemoryComment> MemoryComments => Set<MemoryComment>();

    public DbSet<MomentReaction> MomentReactions => Set<MomentReaction>();

    public DbSet<MomentComment> MomentComments => Set<MomentComment>();

    public DbSet<MomentCommentLike> MomentCommentLikes => Set<MomentCommentLike>();

    public DbSet<UserFollow> UserFollows => Set<UserFollow>();

    public DbSet<UserBlock> UserBlocks => Set<UserBlock>();

    public DbSet<Conversation> Conversations => Set<Conversation>();

    public DbSet<DirectMessage> DirectMessages => Set<DirectMessage>();

    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(
            typeof(ShandisDbContext).Assembly);

        builder.Entity<ShandisUser>(entity =>
        {
            entity.Property(u => u.FirstName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(u => u.LastName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(u => u.DisplayName)
                .HasMaxLength(150);

            entity.Property(u => u.ProfilePictureUrl)
                .HasMaxLength(500);

            entity.Property(u => u.Bio)
                .HasMaxLength(500);
        });

        // PostgreSQL: consistently map DateTime values to timestamptz.
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                var propertyType = Nullable.GetUnderlyingType(
                    property.ClrType) ?? property.ClrType;

                if (propertyType == typeof(DateTime))
                {
                    property.SetColumnType("timestamp with time zone");
                }
            }
        }
    }

}
