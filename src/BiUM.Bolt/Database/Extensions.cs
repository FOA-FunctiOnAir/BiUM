using BiUM.Bolt.Database.Entities;
using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Database;
using Microsoft.EntityFrameworkCore;

namespace BiUM.Bolt.Database;

public static class Extensions
{
    public static async Task<bool> AddOrUpdate<TDbContext, TEntity>(this IBaseBoltDbContext boltDomainDbContext, TDbContext dbContext, string name, TEntity entity, CancellationToken cancellationToken)
        where TDbContext : DbContext
        where TEntity : IBaseEntity
    {
        if (entity is null) return false;

        if (dbContext.GetType().GetProperty(name)?.GetValue(dbContext) is not IQueryable<IBaseEntity> linqQuery) return false;

        var existEntity = await linqQuery.AsNoTracking().Where(x => x.Id == entity.Id).ToListAsync(cancellationToken);

        if (existEntity.Count != 0)
        {
            dbContext.Update(entity);
        }
        else
        {
            dbContext.Add(entity);
        }

        var boltTransaction = new BoltTransaction()
        {
            TableName = name,
            Ids = entity.Id.ToString()
        };

        dbContext.Add(boltTransaction);

        return true;
    }

    public static async Task<bool> AddOrUpdate<TDbContext, TEntity>(this IBaseBoltDbContext boltDomainDbContext, TDbContext dbContext, string name, IList<TEntity> entities, CancellationToken cancellationToken)
        where TDbContext : DbContext
        where TEntity : IBaseEntity
    {
        if (entities is null || entities.Count == 0) return false;

        if (dbContext.GetType().GetProperty(name)?.GetValue(dbContext) is not IQueryable<IBaseEntity> linqQuery) return false;

        var existEntities = await linqQuery.AsNoTracking().Where(x => entities.Select(e => e.Id).Contains(x.Id)).ToListAsync(cancellationToken);

        var insertEntities = entities.Where(x => !existEntities.Select(x => x.Id).Contains(x.Id));
        var updateEntities = entities.Where(x => existEntities.Select(x => x.Id).Contains(x.Id));

        if (insertEntities.Any())
        {
            foreach (var entity in insertEntities)
            {
                dbContext.Add(entity);
            }
        }

        if (updateEntities.Any())
        {
            foreach (var entity in updateEntities)
            {
                dbContext.Update(entity);
            }
        }

        var boltTransaction = new BoltTransaction()
        {
            TableName = name,
            Ids = string.Join(";", entities.Select(e => e.Id.ToString()))
        };

        dbContext.Add(boltTransaction);

        return true;
    }
}