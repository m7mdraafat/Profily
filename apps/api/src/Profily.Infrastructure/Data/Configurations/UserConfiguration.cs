using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Profily.Core.Entities;
using Profily.Core.Enums;

namespace Profily.Infrastructure.Data.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(u => u.GitHubId)
            .IsRequired()
            .HasColumnName("github_id");
        builder.HasIndex(u => u.GitHubId).IsUnique();

        builder.Property(u => u.Username).IsRequired().HasMaxLength(100);
        builder.HasIndex(u => u.Username).IsUnique();

        builder.Property(u => u.DisplayName).HasMaxLength(200);
        builder.Property(u => u.AvatarUrl).HasMaxLength(500);
        builder.Property(u => u.Location).HasMaxLength(200);
        builder.Property(u => u.Bio).HasMaxLength(500);
        builder.Property(u => u.Company).HasMaxLength(200);
        builder.Property(u => u.Email).HasMaxLength(300);
        builder.Property(u => u.GitHubUrl)
            .IsRequired()
            .HasMaxLength(500)
            .HasColumnName("github_url");

        builder.Property(u => u.GitHubTokenEncrypted)
            .IsRequired()
            .HasColumnType("bytea")
            .HasColumnName("github_token_encrypted");

        builder.Property(u => u.ReposCount).HasDefaultValue(0);
        builder.Property(u => u.FollowersCount).HasDefaultValue(0);
        builder.Property(u => u.ContributionsThisYear).HasDefaultValue(0);

        builder.Property(u => u.Plan)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue(PlanType.Free)
            .HasConversion<string>();

        builder.Property(u => u.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(u => u.UpdatedAt).HasDefaultValueSql("now()");
    }
}