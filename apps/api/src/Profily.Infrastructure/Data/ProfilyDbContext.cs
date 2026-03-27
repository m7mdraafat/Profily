using Microsoft.EntityFrameworkCore;

namespace Profily.Infrastructure.Data;

public sealed class ProfilyDbContext : DbContext
{
    public ProfilyDbContext(DbContextOptions<ProfilyDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProfilyDbContext).Assembly);
    }
}