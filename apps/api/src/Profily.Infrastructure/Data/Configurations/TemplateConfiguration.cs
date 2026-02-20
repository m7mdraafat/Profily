using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Profily.Core.Entities;

namespace Profily.Infrastructure.Data.Configurations;

internal sealed class TemplateConfiguration : IEntityTypeConfiguration<Template>
{
    public void Configure(EntityTypeBuilder<Template> builder)
    {
        // String PK (slug) — "3d-purple", "minimal-clean"
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasMaxLength(50);

        builder.Property(t => t.Name).HasMaxLength(100).IsRequired();
        builder.Property(t => t.SectionUrlsJson).HasColumnType("jsonb").IsRequired();

        // PostgreSQL text[] for features and available sections
        builder.Property(t => t.Features).HasColumnType("text[]");
        builder.Property(t => t.AvailableSections).HasColumnType("text[]");

        builder.Property(t => t.CreatedAt).HasDefaultValueSql("now()").IsRequired();
        builder.Property(t => t.UpdatedAt).HasDefaultValueSql("now()").IsRequired();
    }
}
