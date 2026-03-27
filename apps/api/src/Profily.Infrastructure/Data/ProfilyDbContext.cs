using Microsoft.EntityFrameworkCore;
using Profily.Core.Entities;

namespace Profily.Infrastructure.Data;

public sealed class ProfilyDbContext : DbContext
{
    public ProfilyDbContext(DbContextOptions<ProfilyDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<UserSkill> UserSkills => Set<UserSkill>();
    public DbSet<Experience> Experiences => Set<Experience>();
    public DbSet<Education> Educations => Set<Education>();
    public DbSet<SocialLink> SocialLinks => Set<SocialLink>();
    public DbSet<Portfolio> Portfolios => Set<Portfolio>();
    public DbSet<PaymentEvent> PaymentEvents => Set<PaymentEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProfilyDbContext).Assembly);
    }
}