using BiUM.Bolt.Database;
using BiUM.Bolt.Database.Entities;
using BiUM.Core.Common.Configs;
using BiUM.Core.Models;
using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Test.Infrastructure.Persistence;

public class BoltDbContext : TestDbContext, IBoltDbContext
{
    private readonly CorrelationContext _correlationContext;
    private readonly BoltOptions _boltOptions;

    public BoltDbContext(
        CorrelationContext correlationContext,
        DbContextOptions<TestDbContext> dbOptions,
        DbContextOptions<BoltDbContext> boltDbOptions,
        IOptions<BoltOptions> boltOptions,
        BoltEntitySaveChangesInterceptor entitySaveChangesInterceptor
    ) : base(dbOptions, boltDbOptions, entitySaveChangesInterceptor, true)
    {
        _correlationContext = correlationContext;
        _boltOptions = boltOptions.Value;
    }

    public DbSet<BoltTransaction> BoltTransactions => Set<BoltTransaction>();

    public async Task<bool> AddOrUpdate<TEntity>(int order, string name, TEntity entity, CancellationToken cancellationToken)
        where TEntity : IEntity
    {
        return await this.AddOrUpdate(this, _correlationContext, _boltOptions, order, name, entity, cancellationToken);
    }

    public async Task<bool> AddOrUpdate<TEntity>(int order, string name, IList<TEntity> entities, CancellationToken cancellationToken)
        where TEntity : IEntity
    {
        return await this.AddOrUpdate(this, _correlationContext, _boltOptions, order, name, entities, cancellationToken);
    }

    public async Task<bool> Delete<TEntity>(int order, string name, TEntity entity, CancellationToken cancellationToken)
        where TEntity : IEntity
    {
        return await this.Delete(this, _correlationContext, _boltOptions, order, name, entity, cancellationToken);
    }

    public async Task<bool> Delete(int order, string name, Guid id, CancellationToken cancellationToken)
    {
        return await this.Delete(this, _correlationContext, _boltOptions, order, name, id, cancellationToken);
    }

    public async Task<bool> Delete<TEntity>(int order, string name, IList<TEntity> entities, CancellationToken cancellationToken)
        where TEntity : IEntity
    {
        return await this.Delete(this, _correlationContext, _boltOptions, order, name, entities, cancellationToken);
    }

    public async Task<bool> Delete(int order, string name, IList<Guid> ids, CancellationToken cancellationToken)
    {
        return await this.Delete(this, _correlationContext, _boltOptions, order, name, ids, cancellationToken);
    }
}
