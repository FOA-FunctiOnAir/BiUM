using BiUM.Bolt.Database.Entities;
using BiUM.Infrastructure.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace BiUM.Specialized.Database;

public interface IBaseBoltDbContext : IDbContext
{
    DbSet<BoltTransaction> BoltTransactions { get; }

    Task<bool> AddOrUpdate<TEntity>(int order, string name, TEntity entity, CancellationToken cancellationToken) where TEntity : IBaseEntity;
    Task<bool> AddOrUpdate<TEntity>(int order, string name, IList<TEntity> entities, CancellationToken cancellationToken) where TEntity : IBaseEntity;
    Task<bool> Delete<TEntity>(int order, string name, TEntity entity, CancellationToken cancellationToken) where TEntity : IBaseEntity;
    Task<bool> Delete<TEntity>(int order, string name, IList<TEntity> entities, CancellationToken cancellationToken) where TEntity : IBaseEntity;
}

public interface IBaseBoltDomainDbContext : IDbContext
{
    DbSet<BoltStatus> BoltStatuses { get; }
}