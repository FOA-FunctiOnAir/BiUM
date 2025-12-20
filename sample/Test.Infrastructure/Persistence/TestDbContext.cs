using BiUM.Bolt.Database.Entities;
using BiUM.Specialized.Database;
using BiUM.Specialized.Interceptors;
using BiUM.Test.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace BiUM.Test.Infrastructure.Persistence;

public class TestDbContext : BaseDbContext, ITestDbContext
{
    public TestDbContext(
        DbContextOptions<TestDbContext> options,
        EntitySaveChangesInterceptor entitySaveChangesInterceptor
    ) : base(options, entitySaveChangesInterceptor)
    {
    }

    public TestDbContext(
        DbContextOptions<TestDbContext> options,
        DbContextOptions<BoltDbContext> boltOptions,
        BoltEntitySaveChangesInterceptor entitySaveChangesInterceptor,
        bool useBolt
    ) : base(boltOptions, entitySaveChangesInterceptor)
    {
    }

    public DbSet<BoltStatus> BoltStatuses => Set<BoltStatus>();

    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<CurrencyTranslation> CurrencyTranslations => Set<CurrencyTranslation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        modelBuilder.Entity<Currency>().HasIndex(c => c.Deleted);
        modelBuilder.Entity<CurrencyTranslation>().HasIndex(ct => new { ct.RecordId, ct.LanguageId });

        base.OnModelCreating(modelBuilder);
    }
}