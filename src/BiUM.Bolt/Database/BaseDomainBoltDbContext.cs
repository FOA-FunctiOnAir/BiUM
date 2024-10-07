using BiUM.Bolt.Database.Entities;
using BiUM.Infrastructure.Common.Interceptors;
using BiUM.Specialized.Database;
using Microsoft.EntityFrameworkCore;

namespace BiUM.Bolt.Database;

public class BaseDomainBoltDbContext<TDbContext> : BaseDbContext<TDbContext>, IDomainBoltDbContext
    where TDbContext : DbContext
{
    private readonly EntitySaveChangesInterceptor _entitySaveChangesInterceptor;

    public BaseDomainBoltDbContext(
        DbContextOptions<TDbContext> options,
        EntitySaveChangesInterceptor entitySaveChangesInterceptor
    ) : base(options, entitySaveChangesInterceptor)
    {
        _entitySaveChangesInterceptor = entitySaveChangesInterceptor;
    }

    public DbSet<BoltStatus> BoltStatuses => Set<BoltStatus>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(_entitySaveChangesInterceptor);

        base.OnConfiguring(optionsBuilder);
    }

    public async Task<int> SavechangesAsync(CancellationToken cancellationToken = default)
    {
        //_mediator.DispatchDomainEvents(this);
        return await base.SaveChangesAsync(cancellationToken);
    }
}