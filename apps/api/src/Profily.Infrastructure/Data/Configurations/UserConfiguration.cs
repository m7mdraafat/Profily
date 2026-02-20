using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Profily.Core.Entities;

namespace Profily.Infrastructure.Data.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.HasIndex(u => u.GitHubId).IsUnique();
        builder.HasIndex(u => u.Username).IsUnique();

        builder.Property(u => u.Username).HasMaxLength(100).IsRequired();
        builder.Property(u => u.DisplayName).HasMaxLength(200);
        builder.Property(u => u.Email).HasMaxLength(320);
        builder.Property(u => u.Location).HasMaxLength(200);
        builder.Property(u => u.Company).HasMaxLength(200);
        builder.Property(u => u.GitHubUrl).IsRequired();
        builder.Property(u => u.AccessTokenEncrypted).IsRequired();

        // PostgreSQL text[] for social links and languages
        builder.Property(u => u.SocialLinks).HasColumnType("text[]");
        builder.Property(u => u.TopLanguages).HasColumnType("text[]");

        // jsonb for skills — stored as owned JSON collection
        builder.OwnsMany(u => u.Skills, skillBuilder =>
        {
            skillBuilder.ToJson();
        });

        // Timestamps — auto-set
        builder.Property(u => u.CreatedAt).HasDefaultValueSql("now()").IsRequired();
        builder.Property(u => u.UpdatedAt).HasDefaultValueSql("now()").IsRequired();

        // Navigation
        builder.HasMany(u => u.Projects)
            .WithOne(p => p.User)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(u => u.Portfolio)
            .WithOne(p => p.User)
            .HasForeignKey<Portfolio>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
