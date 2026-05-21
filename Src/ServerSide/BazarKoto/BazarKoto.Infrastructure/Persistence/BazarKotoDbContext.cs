using BazarKoto.Application.Interfaces;
using BazarKoto.Domain.Common;
using BazarKoto.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BazarKoto.Infrastructure.Persistence;

public class BazarKotoDbContext : DbContext, IUnitOfWork
{
    public BazarKotoDbContext(DbContextOptions<BazarKotoDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Division> Divisions => Set<Division>();
    public DbSet<District> Districts => Set<District>();
    public DbSet<Upazila> Upazilas => Set<Upazila>();
    public DbSet<UnionOrWard> UnionOrWards => Set<UnionOrWard>();
    public DbSet<Market> Markets => Set<Market>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<PriceSubmission> PriceSubmissions => Set<PriceSubmission>();
    public DbSet<DailyPriceSummary> DailyPriceSummaries => Set<DailyPriceSummary>();
    public DbSet<Contributor> Contributors => Set<Contributor>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<PageVisit> PageVisits => Set<PageVisit>();
    public DbSet<AdminAuditLog> AdminAuditLogs => Set<AdminAuditLog>();
    public DbSet<AdMetric> AdMetrics => Set<AdMetric>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BazarKotoDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
            }

            if (entry.State == EntityState.Modified && entry.Entity is AuditableEntity auditableEntity)
            {
                auditableEntity.UpdatedAt = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
