using BiUM.Infrastructure.Common.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace BiUM.Specialized.Database;

public class BaseDbContext<TDbContext> : DbContext, IDbContext
    where TDbContext : DbContext
{
    private readonly EntitySaveChangesInterceptor _entitySaveChangesInterceptor;

    public BaseDbContext(
        DbContextOptions<TDbContext> options,
        EntitySaveChangesInterceptor entitySaveChangesInterceptor
    ) : base(options)
    {
        _entitySaveChangesInterceptor = entitySaveChangesInterceptor;
    }

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