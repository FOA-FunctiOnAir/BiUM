using BiUM.Bolt.Database.Entities;
using BiUM.Core.Common.Configs;
using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Database;
using Microsoft.EntityFrameworkCore;

namespace BiUM.Bolt.Database;

public static class Extensions
{
    public static async Task<bool> AddOrUpdate<TDbContext, TEntity>(
        this IBaseBoltDbContext boltDomainDbContext,
        TDbContext dbContext,
        BoltOptions boltOptions,
        int order,
        string name,
        TEntity entity,
        CancellationToken cancellationToken)
        where TDbContext : DbContext
        where TEntity : IEntity
    {
        if (boltOptions is null || !boltOptions.Enable || boltOptions.Branch != "Development") return false;

        if (entity is null) return false;

        if (dbContext.GetType().GetProperty(name)?.GetValue(dbContext) is not IQueryable<IEntity> linqQuery) return false;

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
            Ids = entity.Id.ToString(),
            Delete = false,
            SortOrder = order
        };

        dbContext.Add(boltTransaction);

        return true;
    }

    public static async Task<bool> AddOrUpdate<TDbContext, TEntity>(
        this IBaseBoltDbContext boltDomainDbContext,
        TDbContext dbContext,
        BoltOptions boltOptions,
        int order,
        string name,
        IList<TEntity> entities,
        CancellationToken cancellationToken)
        where TDbContext : DbContext
        where TEntity : IEntity
    {
        if (boltOptions is null || !boltOptions.Enable || boltOptions.Branch != "Development") return false;

        if (entities is null || entities.Count == 0) return false;

        if (dbContext.GetType().GetProperty(name)?.GetValue(dbContext) is not IQueryable<IEntity> linqQuery) return false;

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
            Ids = string.Join(";", entities.Select(e => e.Id.ToString()).Distinct()),
            Delete = false,
            SortOrder = order
        };

        dbContext.Add(boltTransaction);

        return true;
    }

    public static async Task<bool> Delete<TDbContext, TEntity>(
        this IBaseBoltDbContext boltDomainDbContext,
        TDbContext dbContext,
        BoltOptions boltOptions,
        int order,
        string name,
        TEntity entity,
        CancellationToken cancellationToken)
        where TDbContext : DbContext
        where TEntity : IEntity
    {
        if (boltOptions is null || !boltOptions.Enable || boltOptions.Branch != "Development") return false;

        if (entity is null) return false;

        if (dbContext.GetType().GetProperty(name)?.GetValue(dbContext) is not IQueryable<IEntity> linqQuery) return false;

        var existEntity = await linqQuery.AsNoTracking().Where(x => x.Id == entity.Id).ToListAsync(cancellationToken);

        if (existEntity.Count != 0)
        {
            if (entity is IBaseEntity baseEntity)
            {
                baseEntity.Deleted = true;

                dbContext.Update(baseEntity);
            }

            var boltTransaction = new BoltTransaction()
            {
                TableName = name,
                Ids = entity.Id.ToString(),
                Delete = true,
                SortOrder = order
            };

            dbContext.Add(boltTransaction);
        }

        return true;
    }

    public static async Task<bool> Delete<TDbContext>(
        this IBaseBoltDbContext boltDomainDbContext,
        TDbContext dbContext,
        BoltOptions boltOptions,
        int order,
        string name,
        Guid id,
        CancellationToken cancellationToken)
        where TDbContext : DbContext
    {
        if (boltOptions is null || !boltOptions.Enable || boltOptions.Branch != "Development") return false;

        if (id == Guid.Empty) return false;

        if (dbContext.GetType().GetProperty(name)?.GetValue(dbContext) is not IQueryable<IEntity> linqQuery) return false;

        var existEntities = await linqQuery.AsNoTracking().Where(x => x.Id == id).ToListAsync(cancellationToken);

        if (existEntities.Count > 0)
        {
            var existEntity = existEntities.FirstOrDefault();

            if (existEntity is IBaseEntity baseEntity)
            {
                baseEntity.Deleted = true;

                dbContext.Update(baseEntity);
            }

            var boltTransaction = new BoltTransaction()
            {
                TableName = name,
                Ids = id.ToString(),
                Delete = true,
                SortOrder = order
            };

            dbContext.Add(boltTransaction);
        }

        return true;
    }

    public static async Task<bool> Delete<TDbContext, TEntity>(
        this IBaseBoltDbContext boltDomainDbContext,
        TDbContext dbContext,
        BoltOptions boltOptions,
        int order,
        string name,
        IList<TEntity> entities,
        CancellationToken cancellationToken)
        where TDbContext : DbContext
        where TEntity : IEntity
    {
        if (boltOptions is null || !boltOptions.Enable || boltOptions.Branch != "Development") return false;

        if (entities is null || entities.Count == 0) return false;

        if (dbContext.GetType().GetProperty(name)?.GetValue(dbContext) is not IQueryable<IEntity> linqQuery) return false;

        var existEntities = await linqQuery.AsNoTracking().Where(x => entities.Select(e => e.Id).Contains(x.Id)).ToListAsync(cancellationToken);

        var deleteEntities = entities.Where(x => existEntities.Select(x => x.Id).Contains(x.Id));

        if (deleteEntities.Any())
        {
            foreach (var entity in deleteEntities)
            {
                if (entity is IBaseEntity baseEntity)
                {
                    baseEntity.Deleted = true;

                    dbContext.Update(baseEntity);
                }
            }

            var boltTransaction = new BoltTransaction()
            {
                TableName = name,
                Ids = string.Join(";", entities.Select(e => e.Id.ToString()).Distinct()),
                Delete = true,
                SortOrder = order
            };

            dbContext.Add(boltTransaction);
        }

        return true;
    }

    public static async Task<bool> Delete<TDbContext>(
        this IBaseBoltDbContext boltDomainDbContext,
        TDbContext dbContext,
        BoltOptions boltOptions,
        int order,
        string name,
        IList<Guid> ids,
        CancellationToken cancellationToken)
        where TDbContext : DbContext
    {
        if (boltOptions is null || !boltOptions.Enable || boltOptions.Branch != "Development") return false;

        if (ids is null || ids.Count == 0) return false;

        if (dbContext.GetType().GetProperty(name)?.GetValue(dbContext) is not IQueryable<IEntity> linqQuery) return false;

        var existEntities = await linqQuery.AsNoTracking().Where(x => ids.Contains(x.Id)).ToListAsync(cancellationToken);

        if (existEntities.Any())
        {
            foreach (var entity in existEntities)
            {
                if (entity is IBaseEntity baseEntity)
                {
                    baseEntity.Deleted = true;

                    dbContext.Update(baseEntity);
                }
            }

            var boltTransaction = new BoltTransaction()
            {
                TableName = name,
                Ids = string.Join(";", existEntities.Select(e => e.Id.ToString()).Distinct()),
                Delete = true,
                SortOrder = order
            };

            dbContext.Add(boltTransaction);
        }

        return true;
    }
}