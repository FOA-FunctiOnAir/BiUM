using BiUM.Bolt.Database.Entities;
using BiUM.Infrastructure.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace BiUM.Specialized.Database;

public interface IBaseBoltDbContext : IDbContext
{
    DbSet<BoltTransaction> BoltTransactions { get; }

    Task<bool> AddOrUpdate<TEntity>(int order, string name, TEntity entity, CancellationToken cancellationToken) where TEntity : IEntity;
    Task<bool> AddOrUpdate<TEntity>(int order, string name, IList<TEntity> entities, CancellationToken cancellationToken) where TEntity : IEntity;
    Task<bool> Delete<TEntity>(int order, string name, TEntity entity, CancellationToken cancellationToken) where TEntity : IEntity;
    Task<bool> Delete<TEntity>(int order, string name, Guid id, CancellationToken cancellationToken) where TEntity : IEntity;
    Task<bool> Delete<TEntity>(int order, string name, IList<TEntity> entities, CancellationToken cancellationToken) where TEntity : IEntity;
    Task<bool> Delete<TEntity>(int order, string name, IList<Guid> ids, CancellationToken cancellationToken) where TEntity : IEntity;
}

public interface IBaseBoltDomainDbContext : IDbContext
{
    DbSet<BoltStatus> BoltStatuses { get; }
}