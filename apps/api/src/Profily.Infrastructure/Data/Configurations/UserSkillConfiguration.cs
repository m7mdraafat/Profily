using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Profily.Core.Entities;
using Profily.Core.Enums;

namespace Profily.Infrastructure.Data.Configurations;

public sealed class UserSkillConfiguration : IEntityTypeConfiguration<UserSkill>
{
    public void Configure(EntityTypeBuilder<UserSkill> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(s => s.UserId).IsRequired();
        builder.Property(s => s.Name).IsRequired().HasMaxLength(100);

        builder.Property(s => s.Category)
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion<string>();

        builder.Property(s => s.Confidence)
            .IsRequired()
            .HasPrecision(3, 2);

        builder.Property(s => s.IconFilename).HasMaxLength(100);

        builder.Property(s => s.Source)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue(SkillSource.Inferred)
            .HasConversion<string>();

        builder.Property(s => s.DisplayOrder).HasDefaultValue(0);

        builder.Property(s => s.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(s => s.UpdatedAt).HasDefaultValueSql("now()");

        builder.HasIndex(s => s.UserId);
        builder.HasIndex(s => s.SourceRepoId);
        builder.HasIndex(s => new { s.UserId, s.Name, s.Category }).IsUnique();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(s => s.SourceRepoId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}