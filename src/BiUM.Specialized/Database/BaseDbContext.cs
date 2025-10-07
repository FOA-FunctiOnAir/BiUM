using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Interceptors;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BiUM.Specialized.Database;

public class BaseDbContext : DbContext, IDbContext
{
    public bool HardDelete = false;

    private readonly EntitySaveChangesInterceptor _entitySaveChangesInterceptor;

    public BaseDbContext(
        DbContextOptions<DbContext> options,
        EntitySaveChangesInterceptor entitySaveChangesInterceptor
    ) : base(options)
    {
        _entitySaveChangesInterceptor = entitySaveChangesInterceptor;
    }

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
        if (_entitySaveChangesInterceptor != null)
            optionsBuilder.AddInterceptors(_entitySaveChangesInterceptor);

        base.OnConfiguring(optionsBuilder);
    }

    public async Task<int> SavechangesAsync(CancellationToken cancellationToken = default)
    {
        return await base.SaveChangesAsync(cancellationToken);
    }
}