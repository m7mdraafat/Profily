using Microsoft.EntityFrameworkCore;
using Profily.Core.Entities;

namespace Profily.Infrastructure.Data;

public sealed class ProfilyDbContext : DbContext
{
    public ProfilyDbContext(DbContextOptions<ProfilyDbContext> options)
        : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Portfolio> Portfolios => Set<Portfolio>();
    public DbSet<Template> Templates => Set<Template>();
    public DbSet<PortfolioDeployment> Deployments => Set<PortfolioDeployment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply all IEntityTypeConfiguration<T> from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProfilyDbContext).Assembly);
    }
}
