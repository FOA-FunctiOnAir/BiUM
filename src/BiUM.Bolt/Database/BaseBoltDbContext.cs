using BiUM.Bolt.Database.Entities;
using BiUM.Infrastructure.Common.Interceptors;
using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Database;
using Microsoft.EntityFrameworkCore;

namespace BiUM.Bolt.Database;

public class BaseBoltDbContext<TDbContext> : BaseDbContext<TDbContext>, IBoltDbContext
    where TDbContext : DbContext
{
    private readonly EntitySaveChangesInterceptor _entitySaveChangesInterceptor;

    public BaseBoltDbContext(
        DbContextOptions<TDbContext> options,
        EntitySaveChangesInterceptor entitySaveChangesInterceptor
    ) : base(options, entitySaveChangesInterceptor)
    {
        _entitySaveChangesInterceptor = entitySaveChangesInterceptor;
    }

    public DbSet<BoltTransaction> BoltTransactions => Set<BoltTransaction>();

    public async Task<bool> AddOrUpdate<TEntity>(string name, TEntity entity, CancellationToken cancellationToken)
        where TEntity : IBaseEntity
    {
        if (entity is null) return false;

        var linqQuery = this.GetType().GetProperty(name)?.GetValue(this) as IQueryable<IBaseEntity>;

        var existEntity = await linqQuery.AsNoTracking().Where(x => x.Id == entity.Id).ToListAsync(cancellationToken);

        if (existEntity.Any())
        {
            this.Update(entity);
        }
        else
        {
            this.Add(entity);
        }

        var boltTransaction = new BoltTransaction()
        {
            TableName = name,
            Ids = entity.Id.ToString()
        };

        this.Add(boltTransaction);

        return true;
    }

    public async Task<bool> AddOrUpdate<TEntity>(string name, IList<TEntity> entities, CancellationToken cancellationToken)
        where TEntity : IBaseEntity
    {
        if (entities is null || entities.Count == 0) return false;

        var linqQuery = this.GetType().GetProperty(name)?.GetValue(this) as IQueryable<IBaseEntity>;

        var existEntities = await linqQuery.AsNoTracking().Where(x => entities.Select(e => e.Id).Contains(x.Id)).ToListAsync(cancellationToken);

        var insertEntities = entities.Where(x => !existEntities.Select(x => x.Id).Contains(x.Id));
        var updateEntities = entities.Where(x => existEntities.Select(x => x.Id).Contains(x.Id));

        if (insertEntities.Any())
        {
            foreach (var entity in insertEntities)
            {
                this.Add(entity);
            }
        }

        if (updateEntities.Any())
        {
            foreach (var entity in updateEntities)
            {
                this.Update(entity);
            }
        }

        var boltTransaction = new BoltTransaction()
        {
            TableName = name,
            Ids = string.Join(";", entities.Select(e => e.Id.ToString()))
        };

        this.Add(boltTransaction);

        return true;
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