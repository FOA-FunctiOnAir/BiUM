using BiUM.Bolt.Database.Entities;
using BiUM.Infrastructure.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace BiUM.Specialized.Database;

public interface IBoltDbContext : IDbContext
{
    DbSet<BoltTransaction> BoltTransactions { get; }

    Task<bool> AddOrUpdate<TEntity>(string name, TEntity entity, CancellationToken cancellationToken) where TEntity : IBaseEntity;
    Task<bool> AddOrUpdate<TEntity>(string name, IList<TEntity> entities, CancellationToken cancellationToken) where TEntity : IBaseEntity;
}

public interface IDomainBoltDbContext : IDbContext
{
    DbSet<BoltStatus> BoltStatuses { get; }
}