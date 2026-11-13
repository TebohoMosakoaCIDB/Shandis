
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shandis.Domain.Entities;

namespace Shandis.Infrastructure.Persistence.Configurations;

public class MemoryCategoryConfiguration
    : IEntityTypeConfiguration<MemoryCategory>
{
    public void Configure(EntityTypeBuilder<MemoryCategory> builder)
    {
        builder.ToTable("MemoryCategories");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.HasIndex(x => x.Name)
            .IsUnique();
    }
}
