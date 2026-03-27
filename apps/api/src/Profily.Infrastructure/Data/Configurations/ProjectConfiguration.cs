using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Profily.Core.Entities;

namespace Profily.Infrastructure.Data.Configurations;

public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(p => p.UserId).IsRequired();
        builder.Property(p => p.GitHubRepoId)
            .IsRequired()
            .HasColumnName("github_repo_id");
        builder.Property(p => p.Name).IsRequired().HasMaxLength(300);
        builder.Property(p => p.CustomDescription);
        builder.Property(p => p.Language).HasMaxLength(100);
        builder.Property(p => p.Topics).HasColumnType("text[]");
        builder.Property(p => p.Stars).HasDefaultValue(0);
        builder.Property(p => p.Forks).HasDefaultValue(0);
        builder.Property(p => p.IsFork).HasDefaultValue(false);
        builder.Property(p => p.HtmlUrl).IsRequired().HasMaxLength(500);
        builder.Property(p => p.HomepageUrl).HasMaxLength(500);
        builder.Property(p => p.IsEnabled).HasDefaultValue(false);
        builder.Property(p => p.DisplayOrder).HasDefaultValue(0);
        builder.Property(p => p.SkillsHash).HasMaxLength(64);

        builder.Property(p => p.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(p => p.UpdatedAt).HasDefaultValueSql("now()");

        builder.HasIndex(p => p.UserId);
        builder.HasIndex(p => new { p.UserId, p.IsEnabled })
            .HasFilter("is_enabled = true");
        
        builder.HasIndex(p => new { p.UserId, p.GitHubRepoId }).IsUnique();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}