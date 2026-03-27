using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Profily.Core.Entities;

namespace Profily.Infrastructure.Data.Configurations;

public sealed class SocialLinkConfiguration : IEntityTypeConfiguration<SocialLink>
{
    public void Configure(EntityTypeBuilder<SocialLink> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(s => s.UserId).IsRequired();
        builder.Property(s => s.Platform).IsRequired().HasMaxLength(50);
        builder.Property(s => s.Url).IsRequired().HasMaxLength(500);
        builder.Property(s => s.IconFilename).HasMaxLength(100);
        builder.Property(s => s.DisplayOrder).HasDefaultValue(0);

        builder.Property(s => s.CreatedAt).HasDefaultValueSql("now()");
        builder.HasIndex(s => s.UserId);
        builder.HasIndex(s => new { s.UserId, s.Platform }).IsUnique();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}