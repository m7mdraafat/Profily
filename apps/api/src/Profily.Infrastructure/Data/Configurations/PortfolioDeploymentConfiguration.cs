using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Profily.Core.Entities;
using Profily.Core.Enums;

namespace Profily.Infrastructure.Data.Configurations;

internal sealed class PortfolioDeploymentConfiguration : IEntityTypeConfiguration<PortfolioDeployment>
{
    public void Configure(EntityTypeBuilder<PortfolioDeployment> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(d => d.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(DeploymentStatus.Pending)
            .IsRequired();

        builder.Property(d => d.CommitSha).HasMaxLength(40);
        builder.Property(d => d.StartedAt).HasDefaultValueSql("now()").IsRequired();

        // Index for finding in-progress deployments
        builder.HasIndex(d => d.Status)
            .HasFilter("\"Status\" IN ('Pending', 'Deploying')");
    }
}
