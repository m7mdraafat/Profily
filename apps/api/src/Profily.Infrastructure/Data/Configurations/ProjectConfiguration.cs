using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Profily.Core.Entities;

namespace Profily.Infrastructure.Data.Configurations;

internal sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasDefaultValueSql("gen_random_uuid()");

        // Unique per user — can't have duplicate GitHub repos
        builder.HasIndex(p => new { p.UserId, p.GitHubRepoId }).IsUnique();

        // Filtered index — fast lookup of selected projects per user
        builder.HasIndex(p => new { p.UserId, p.IsSelected })
            .HasFilter("\"IsSelected\" = true");

        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Language).HasMaxLength(50);
        builder.Property(p => p.HtmlUrl).IsRequired();
        builder.Property(p => p.SyncedAt).HasDefaultValueSql("now()").IsRequired();

        // PostgreSQL text[] for topics
        builder.Property(p => p.Topics).HasColumnType("text[]");
    }
}
