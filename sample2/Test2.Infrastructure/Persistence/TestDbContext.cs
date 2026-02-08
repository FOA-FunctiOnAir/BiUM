using BiApp.Test2.Domain.Entities;
using BiUM.Bolt.Database.Entities;
using BiUM.Specialized.Database;
using BiUM.Specialized.Interceptors;
using Microsoft.EntityFrameworkCore;
using System;

namespace BiApp.Test2.Infrastructure.Persistence;

public class TestDbContext : BaseDbContext, ITestDbContext
{
    public TestDbContext(
        IServiceProvider serviceProvider,
        DbContextOptions<TestDbContext> options,
        EntitySaveChangesInterceptor entitySaveChangesInterceptor
    ) : base(serviceProvider, options, entitySaveChangesInterceptor)
    {
    }

    public TestDbContext(
        IServiceProvider serviceProvider,
        DbContextOptions<TestDbContext> options,
        DbContextOptions<BoltDbContext> boltOptions,
        BoltEntitySaveChangesInterceptor entitySaveChangesInterceptor,
        bool useBolt
    ) : base(serviceProvider, boltOptions, entitySaveChangesInterceptor)
    {
    }

    public DbSet<BoltStatus> BoltStatuses => Set<BoltStatus>();

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<AccountTranslation> AccountTranslations => Set<AccountTranslation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfigurationsFromAssembly(typeof(IInfrastructureMarker).Assembly);

        _ = modelBuilder.Entity<AccountTranslation>().HasIndex(ct => new { ct.RecordId, ct.LanguageId });

        base.OnModelCreating(modelBuilder);
    }
}