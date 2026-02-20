using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Profily.Core.Entities;
using Profily.Core.Enums;

namespace Profily.Infrastructure.Data.Configurations;

internal sealed class PortfolioConfiguration : IEntityTypeConfiguration<Portfolio>
{
    public void Configure(EntityTypeBuilder<Portfolio> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasDefaultValueSql("gen_random_uuid()");

        // One portfolio per user (MVP)
        builder.HasIndex(p => p.UserId).IsUnique();

        builder.Property(p => p.TemplateId).HasMaxLength(50).IsRequired();
        builder.Property(p => p.CustomizationsJson).HasColumnType("jsonb").IsRequired();

        // Enum stored as string in PostgreSQL
        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(PortfolioStatus.Draft)
            .IsRequired();

        builder.Property(p => p.CreatedAt).HasDefaultValueSql("now()").IsRequired();
        builder.Property(p => p.UpdatedAt).HasDefaultValueSql("now()").IsRequired();

        // Navigation
        builder.HasOne(p => p.Template)
            .WithMany(t => t.Portfolios)
            .HasForeignKey(p => p.TemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Deployments)
            .WithOne(d => d.Portfolio)
            .HasForeignKey(d => d.PortfolioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
