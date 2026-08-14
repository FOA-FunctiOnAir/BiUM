using BiUM.Core.Authorization;
using BiUM.Core.Common.Configs;
using BiUM.Core.Compensation;
using BiUM.Infrastructure.Common.Models;
using BiUM.Infrastructure.Persistence.Extensions;
using BiUM.Specialized.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace BiUM.Specialized.Database;

public class BaseDbContext : DbContext, IDbContext
{
    private bool _hardDeleteEnabled;

    private readonly IServiceProvider _serviceProvider;
    private readonly EntitySaveChangesInterceptor? _entitySaveChangesInterceptor;
    private readonly BoltEntitySaveChangesInterceptor? _boltEntitySaveChangesInterceptor;
    private readonly ICorrelationContextAccessor? _correlationContextAccessor;
    protected BiAppOptions BiAppOptions { get; }

    // L-5: compiled open delegates per entity type replace MakeGenericMethod().Invoke() on every model build.
    private static readonly MethodInfo _applyCompensationMethodDef =
        typeof(BaseDbContext).GetMethod(nameof(ApplyCompensationFilter), BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static readonly MethodInfo _applyReadableMethodDef =
        typeof(BaseDbContext).GetMethod(nameof(ApplyReadableCompensationFilter), BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static readonly ConcurrentDictionary<Type, Action<BaseDbContext, ModelBuilder>> _compensationFilterDelegates = new();
    private static readonly ConcurrentDictionary<Type, Action<BaseDbContext, ModelBuilder>> _readableFilterDelegates = new();

    public BaseDbContext(
        IServiceProvider serviceProvider,
        DbContextOptions options,
        EntitySaveChangesInterceptor entitySaveChangesInterceptor
    ) : base(options)
    {
        _serviceProvider = serviceProvider;
        _entitySaveChangesInterceptor = entitySaveChangesInterceptor;
        _correlationContextAccessor = serviceProvider.GetService<ICorrelationContextAccessor>();

        BiAppOptions = serviceProvider.GetRequiredService<IOptions<BiAppOptions>>().Value;
    }

    public BaseDbContext(
        IServiceProvider serviceProvider,
        DbContextOptions options,
        BoltEntitySaveChangesInterceptor boltEntitySaveChangesInterceptor
    ) : base(options)
    {
        _serviceProvider = serviceProvider;
        _boltEntitySaveChangesInterceptor = boltEntitySaveChangesInterceptor;
        _correlationContextAccessor = serviceProvider.GetService<ICorrelationContextAccessor>();

        BiAppOptions = serviceProvider.GetRequiredService<IOptions<BiAppOptions>>().Value;
    }

    public DbSet<DomainCompensationSnapshot> DomainCompensationSnapshots => Set<DomainCompensationSnapshot>();
    public DbSet<DomainPendingEvent> DomainPendingEvents => Set<DomainPendingEvent>();
    public DbSet<DomainCrud> DomainCruds => Set<DomainCrud>();
    public DbSet<DomainCrudColumn> DomainCrudColumns => Set<DomainCrudColumn>();
    public DbSet<DomainCrudPartialUpdate> DomainCrudPartialUpdates => Set<DomainCrudPartialUpdate>();
    public DbSet<DomainCrudPartialUpdateColumn> DomainCrudPartialUpdateColumns => Set<DomainCrudPartialUpdateColumn>();
    public DbSet<DomainCrudTranslation> DomainCrudTranslations => Set<DomainCrudTranslation>();
    public DbSet<DomainCrudVersion> DomainCrudVersions => Set<DomainCrudVersion>();
    public DbSet<DomainCrudVersionColumn> DomainCrudVersionColumns => Set<DomainCrudVersionColumn>();
    public DbSet<DomainCrudVersionPartialUpdate> DomainCrudVersionPartialUpdates => Set<DomainCrudVersionPartialUpdate>();
    public DbSet<DomainCrudVersionPartialUpdateColumn> DomainCrudVersionPartialUpdateColumns => Set<DomainCrudVersionPartialUpdateColumn>();
    //public DbSet<DomainDynamicApi> DomainDynamicApis => Set<DomainDynamicApi>();
    //public DbSet<DomainDynamicApiParameter> DomainDynamicApiParameters => Set<DomainDynamicApiParameter>();
    //public DbSet<DomainDynamicApiTranslation> DomainDynamicApiTranslations => Set<DomainDynamicApiTranslation>();
    //public DbSet<DomainDynamicApiVersion> DomainDynamicApiVersions => Set<DomainDynamicApiVersion>();
    //public DbSet<DomainDynamicApiVersionParameter> DomainDynamicApiVersionParameters => Set<DomainDynamicApiVersionParameter>();
    public DbSet<DomainTranslation> DomainTranslations => Set<DomainTranslation>();
    public DbSet<DomainTranslationDetail> DomainTranslationDetails => Set<DomainTranslationDetail>();

    public bool HardDeleteEnabled => _hardDeleteEnabled;

    protected void EnableHardDelete()
    {
        _hardDeleteEnabled = true;
    }

    protected void DisableHardDelete()
    {
        _hardDeleteEnabled = false;
    }

    // Çağrıldığı anda AsyncLocal'dan aktif session ID'yi okur.
    // Global query filter expression'larında 'this.GetCurrentCompensationSessionId()' olarak
    // referans edilir; EF Core her sorgu çevrimiyle DbContext instance'ına karşı değerlendirir.
    // _correlationContextAccessor constructor'da cache'lendi — her sorgu için DI lookup yapılmaz.
    private Guid? GetCurrentCompensationSessionId()
        => _correlationContextAccessor?.CorrelationContext?.CompensationSessionId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DomainCompensationSnapshot>().HasIndex(c => c.CompensationSessionId);
        modelBuilder.Entity<DomainCompensationSnapshot>().HasIndex(c => new { c.EntityId, c.CompensationSessionId });
        modelBuilder.Entity<DomainPendingEvent>().HasIndex(e => new { e.CompensationSessionId, e.Dispatched });
        modelBuilder.Entity<DomainCrud>().HasIndex(c => c.Deleted);
        modelBuilder.Entity<DomainCrudColumn>().HasIndex(c => c.Deleted);
        modelBuilder.Entity<DomainCrudPartialUpdate>().HasIndex(c => new { c.CrudId, c.Code }).IsUnique();
        modelBuilder.Entity<DomainCrudVersionPartialUpdate>().HasIndex(c => new { c.CrudVersionId, c.Code }).IsUnique();
        modelBuilder.Entity<DomainCrudVersion>().HasIndex(c => c.Deleted);
        modelBuilder.Entity<DomainCrudVersionColumn>().HasIndex(c => c.Deleted);
        modelBuilder.Entity<DomainTranslation>().HasIndex(c => c.Deleted);
        modelBuilder.Entity<DomainTranslationDetail>().HasIndex(c => c.Deleted);

        // DomainDynamicApi* CLR types remain in Infrastructure; excluded from migrations until DbSets + IDbContext are re-enabled above.
        //modelBuilder.Ignore<DomainDynamicApi>();
        //modelBuilder.Ignore<DomainDynamicApiParameter>();
        //modelBuilder.Ignore<DomainDynamicApiTranslation>();
        //modelBuilder.Ignore<DomainDynamicApiVersion>();
        //modelBuilder.Ignore<DomainDynamicApiVersionParameter>();

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(IBaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var deletedProp = Expression.Property(parameter, nameof(BaseEntity.Deleted));
            modelBuilder.Entity(entityType.ClrType).HasIndex(deletedProp.Member.Name);

            if (typeof(ICompensation).IsAssignableFrom(entityType.ClrType))
            {
                var del = _compensationFilterDelegates.GetOrAdd(entityType.ClrType, static (t, m) =>
                {
                    var selfParam = Expression.Parameter(typeof(BaseDbContext), "self");
                    var mbParam = Expression.Parameter(typeof(ModelBuilder), "mb");
                    var call = Expression.Call(selfParam, m.MakeGenericMethod(t), mbParam);
                    return Expression.Lambda<Action<BaseDbContext, ModelBuilder>>(call, selfParam, mbParam).Compile();
                }, _applyCompensationMethodDef);
                del(this, modelBuilder);
            }
            else if (typeof(IReadableCompensation).IsAssignableFrom(entityType.ClrType))
            {
                var del = _readableFilterDelegates.GetOrAdd(entityType.ClrType, static (t, m) =>
                {
                    var selfParam = Expression.Parameter(typeof(BaseDbContext), "self");
                    var mbParam = Expression.Parameter(typeof(ModelBuilder), "mb");
                    var call = Expression.Call(selfParam, m.MakeGenericMethod(t), mbParam);
                    return Expression.Lambda<Action<BaseDbContext, ModelBuilder>>(call, selfParam, mbParam).Compile();
                }, _applyReadableMethodDef);
                del(this, modelBuilder);
            }
            else
            {
                var filter = Expression.Lambda(Expression.Equal(deletedProp, Expression.Constant(false)), parameter);
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
            }
        }

        if (!string.IsNullOrEmpty(BiAppOptions?.EncryptionKey))
        {
            modelBuilder.ApplyEncryptedDataConversion(BiAppOptions.EncryptionKey);
        }

        base.OnModelCreating(modelBuilder);
    }

    // ICompensation: sadece commit edilmiş veya kendi session'ına ait pending kayıtlar görünür.
    private void ApplyCompensationFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, IBaseEntity, ICompensation
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e =>
            !e.Deleted &&
            (e.CStatus == null ||
             e.CStatus == CompensationStatusCodes.Committed ||
             e.CompensationSessionId == GetCurrentCompensationSessionId()));
    }

    // IReadableCompensation: commit edilmiş + UR/DR (herkese açık pending) + kendi session'ının pending kayıtları görünür.
    private void ApplyReadableCompensationFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, IBaseEntity, IReadableCompensation
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e =>
            !e.Deleted &&
            (e.CStatus == null ||
             e.CStatus == CompensationStatusCodes.Committed ||
             e.CStatus == CompensationStatusCodes.UpdateReadable ||
             e.CStatus == CompensationStatusCodes.DeleteReadable ||
             e.CompensationSessionId == GetCurrentCompensationSessionId()));
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (_entitySaveChangesInterceptor is not null)
        {
            optionsBuilder.AddInterceptors(_entitySaveChangesInterceptor);
        }

        if (_boltEntitySaveChangesInterceptor is not null)
        {
            optionsBuilder.AddInterceptors(_boltEntitySaveChangesInterceptor);
        }

        base.OnConfiguring(optionsBuilder);
    }
}