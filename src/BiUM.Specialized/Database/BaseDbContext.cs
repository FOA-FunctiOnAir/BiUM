using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Interceptors;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Database;

public class BaseDbContext : DbContext, IDbContext
{
    public bool HardDelete = false;

    private readonly EntitySaveChangesInterceptor _entitySaveChangesInterceptor;
    private readonly BoltEntitySaveChangesInterceptor _boltEntitySaveChangesInterceptor;

    public BaseDbContext(
        DbContextOptions options,
        EntitySaveChangesInterceptor entitySaveChangesInterceptor
    ) : base(options)
    {
        _entitySaveChangesInterceptor = entitySaveChangesInterceptor;
    }

    public BaseDbContext(
        DbContextOptions options,
        BoltEntitySaveChangesInterceptor boltEntitySaveChangesInterceptor
    ) : base(options)
    {
        _boltEntitySaveChangesInterceptor = boltEntitySaveChangesInterceptor;
    }

    public DbSet<DomainCrud> DomainCruds => Set<DomainCrud>();
    public DbSet<DomainCrudColumn> DomainCrudColumns => Set<DomainCrudColumn>();
    public DbSet<DomainCrudTranslation> DomainCrudTranslations => Set<DomainCrudTranslation>();
    public DbSet<DomainCrudVersion> DomainCrudVersions => Set<DomainCrudVersion>();
    public DbSet<DomainCrudVersionColumn> DomainCrudVersionColumns => Set<DomainCrudVersionColumn>();
    public DbSet<DomainDynamicApi> DomainDynamicApis => Set<DomainDynamicApi>();
    public DbSet<DomainDynamicApiParameter> DomainDynamicApiParameters => Set<DomainDynamicApiParameter>();
    public DbSet<DomainDynamicApiTranslation> DomainDynamicApiTranslations => Set<DomainDynamicApiTranslation>();
    public DbSet<DomainDynamicApiVersion> DomainDynamicApiVersions => Set<DomainDynamicApiVersion>();
    public DbSet<DomainDynamicApiVersionParameter> DomainDynamicApiVersionParameters => Set<DomainDynamicApiVersionParameter>();
    public DbSet<DomainTranslation> DomainTranslations => Set<DomainTranslation>();
    public DbSet<DomainTranslationDetail> DomainTranslationDetails => Set<DomainTranslationDetail>();

    protected void OpenHardDelete()
    {
        HardDelete = true;
    }

    protected void CloseHardDelete()
    {
        HardDelete = false;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DomainCrud>().HasIndex(c => c.Deleted);
        modelBuilder.Entity<DomainCrudColumn>().HasIndex(c => c.Deleted);
        modelBuilder.Entity<DomainCrudVersion>().HasIndex(c => c.Deleted);
        modelBuilder.Entity<DomainCrudVersionColumn>().HasIndex(c => c.Deleted);
        modelBuilder.Entity<DomainTranslation>().HasIndex(c => c.Deleted);
        modelBuilder.Entity<DomainTranslationDetail>().HasIndex(c => c.Deleted);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(IBaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var prop = Expression.Property(parameter, nameof(BaseEntity.Deleted));
                var filter = Expression.Lambda(Expression.Equal(prop, Expression.Constant(false)), parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
            }
        }

        base.OnModelCreating(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (_entitySaveChangesInterceptor is not null)
        {
            _ = optionsBuilder.AddInterceptors(_entitySaveChangesInterceptor);
        }
        if (_boltEntitySaveChangesInterceptor is not null)
        {
            _ = optionsBuilder.AddInterceptors(_boltEntitySaveChangesInterceptor);
        }

        base.OnConfiguring(optionsBuilder);
    }

    public async Task<int> SavechangesAsync(CancellationToken cancellationToken = default)
    {
        return await base.SaveChangesAsync(cancellationToken);
    }
}
