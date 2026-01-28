using BiUM.Bolt.Database.Entities;
using BiUM.Core.Common.Configs;
using BiUM.Core.Common.Utils;
using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Bolt.Database;

public class BoltDbContextInitialiser<TBoltDbContext, TDbContext> : DbContextInitialiser<TDbContext>, IBaseBoltDbContextInitialiser
    where TBoltDbContext : DbContext
    where TDbContext : DbContext
{
    private readonly BoltOptions _boltOptions;
    private readonly TBoltDbContext _boltContext;
    private static readonly ConcurrentDictionary<Type, Func<DbContext, IQueryable>> QueryFactoryCache = new();

    public BoltDbContextInitialiser(ILogger<BoltDbContextInitialiser<TBoltDbContext, TDbContext>> logger, IOptions<BoltOptions> boltOptions, TBoltDbContext boltContext, TDbContext dbContext)
        : base(dbContext, logger)
    {
        _boltOptions = boltOptions.Value;
        _boltContext = boltContext;
    }

    public virtual async Task InitialiseAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _ = await _boltContext.Database.EnsureCreatedAsync(cancellationToken);

            if (_boltContext.Database.IsSqlServer())
            {
                await _boltContext.Database.MigrateAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error occurred while initialising the bolt database.");

            throw;
        }
    }

    public virtual async Task EqualizeAsync(CancellationToken cancellationToken = default)
    {
        if (!_boltOptions.Enable)
        {
            return;
        }

        BoltStatus? boltStatus = null;
        BoltTransaction? lastTransaction = null;

        var boltStatuses = await GetResultsFromTable<TDbContext, BoltStatus>(DbContext, $"SELECT * FROM dbo.\"__BOLT_STATUS\" WHERE \"ACTIVE\" = true", cancellationToken);

        if (boltStatuses.Any())
        {
            boltStatus = boltStatuses[0];
        }

        if (boltStatus?.LastTransactionId is not null)
        {
            var boltTransactions = await GetResultsFromTable<TBoltDbContext, BoltTransaction>(
                _boltContext,
                FormattableStringFactory.Create($"SELECT * FROM dbo.\"__BOLT_TRANSACTION\" WHERE \"ID\" = '{boltStatus.LastTransactionId}'"),
                cancellationToken);

            if (boltTransactions.Any())
            {
                lastTransaction = boltTransactions[0];
            }
        }

        var lastTransactionQueryStatement = lastTransaction is null ? string.Empty : $" WHERE \"ID\" != '{lastTransaction.Id}' AND (\"CREATED\" > '{lastTransaction.Created:yyyy-MM-dd}' or (\"CREATED\" = '{lastTransaction.Created:yyyy-MM-dd}' and \"CREATED_TIME\" > '{lastTransaction.CreatedTime:HH:mm:ss}'))";
        var lastTransactionQuery = $"SELECT * FROM dbo.\"__BOLT_TRANSACTION\"" + (string.IsNullOrEmpty(lastTransactionQueryStatement) ? "" : lastTransactionQueryStatement) + " ORDER BY \"CREATED\" ASC, \"SORT_ORDER\" ASC, \"CREATED_TIME\" ASC";

        var allTransactions = await GetResultsFromTable<TBoltDbContext, BoltTransaction>(
            _boltContext,
            FormattableStringFactory.Create(lastTransactionQuery),
            cancellationToken);

        if (!allTransactions.Any())
        {
            return;
        }

        var isError = false;
        var transactionId = Guid.Empty;
        Guid? lastTransactionId = null;
        Guid? lastCorrelationId = null;
        List<Guid> executedCorrelations = [];

        try
        {
            foreach (var mainTransaction in allTransactions)
            {
                if (executedCorrelations.Contains(mainTransaction.CorrelationId))
                {
                    continue;
                }

                lastCorrelationId = mainTransaction.CorrelationId;

                var groupTransactions = allTransactions
                    .Where(t => t.CorrelationId == mainTransaction.CorrelationId)
                    .OrderBy(t => t.Created.ToDateTime(t.CreatedTime))
                    .ThenBy(t => t.SortOrder)
                    .ToList();

                foreach (var transaction in groupTransactions)
                {
                    transactionId = transaction.Id;

                    var ids = transaction.Ids?.Trim().Split(";");
                    var guidIds = ids?.Select(x => new Guid(x.Trim()));
                    var allIds = guidIds ?? [];

                    var uniqueIds = allIds.Distinct().ToArray();

                    if (uniqueIds.Length == 0)
                    {
                        continue;
                    }

                    var boltContextDbSetProp = _boltContext.GetType().GetProperty(transaction.TableName);

                    if (boltContextDbSetProp is null)
                    {
                        continue;
                    }

                    var boltEntityClrType = boltContextDbSetProp.PropertyType.GetGenericArguments()[0];

                    var boltQuery = GetQueryableIgnoringFilters(_boltContext, boltEntityClrType);

                    var boltEntities = await boltQuery
                        .Cast<IEntity>()
                        .Where(x => uniqueIds.Contains(x.Id))
                        .ToListAsync(cancellationToken);

                    var contextDbSetProp = DbContext.GetType().GetProperty(transaction.TableName);

                    if (contextDbSetProp is null)
                    {
                        continue;
                    }

                    var targetEntityClrType = contextDbSetProp.PropertyType.GetGenericArguments()[0];

                    var targetQuery = GetQueryableIgnoringFilters(DbContext, targetEntityClrType);

                    var targetEntities = await targetQuery
                        .Cast<IEntity>()
                        .Where(x => uniqueIds.Contains(x.Id))
                        .ToListAsync(cancellationToken);

                    var entityType = _boltContext.Model.FindEntityType(boltEntityClrType);
                    var hasParentId = entityType?.FindProperty("ParentId") is not null;

                    if (hasParentId)
                    {
                        boltEntities = OrderHierarchically(boltEntities, boltEntityClrType);
                    }

                    if (transaction.Delete)
                    {
                        var deleteEntities = boltEntities.Where(b => targetEntities.Any(t => t.Id == b.Id));

                        DbContext.RemoveRange(deleteEntities);
                    }
                    else
                    {
                        var insertEntities = boltEntities.Where(b => targetEntities.All(t => t.Id != b.Id));
                        var updateEntities = boltEntities.Where(b => targetEntities.Any(t => t.Id == b.Id));

                        DbContext.AddRange(insertEntities);
                        DbContext.UpdateRange(updateEntities);
                    }
                }

                _ = await DbContext.SaveChangesAsync(cancellationToken);

                lastTransactionId = transactionId;

                DbContext.ChangeTracker.Clear();

                executedCorrelations.Add(mainTransaction.CorrelationId);
            }
        }
        catch (Exception ex)
        {
            isError = true;

            DbContext.ChangeTracker.Clear();

            if (boltStatus is not null)
            {
                boltStatus.Active = false;

                _ = DbContext.Update(boltStatus);
            }

            var newBoltStatus = new BoltStatus
            {
                Id = GuidGenerator.New(),
                Active = true,
                LastTransactionId = lastTransactionId ?? boltStatus?.LastTransactionId,
                Error = $"CorrelationId:{lastCorrelationId}, TransactionId:{transactionId}, Message:{ex.ToString()}"
            };

            _ = DbContext.Add(newBoltStatus);

            _ = await DbContext.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            if (!isError)
            {
                var last = allTransactions
                    .OrderByDescending(x => x.Created)
                    .ThenByDescending(x => x.CreatedTime)
                    .ThenByDescending(x => x.SortOrder)
                    .First();

                if (boltStatus is not null)
                {
                    boltStatus.Active = false;

                    _ = DbContext.Update(boltStatus);
                }

                var newBoltStatus = new BoltStatus
                {
                    Id = GuidGenerator.New(),
                    Active = true,
                    LastTransactionId = last.Id,
                    Error = null
                };

                _ = DbContext.Add(newBoltStatus);

                _ = await DbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private static IQueryable GetQueryableIgnoringFilters(DbContext context, Type entityClrType)
    {
        var factory = QueryFactoryCache.GetOrAdd(entityClrType, type =>
        {
            var setMethod = typeof(DbContext)
                .GetMethods()
                .Single(m =>
                    m.Name == nameof(Microsoft.EntityFrameworkCore.DbContext.Set) &&
                    m.IsGenericMethod &&
                    m.GetParameters().Length == 0)
                .MakeGenericMethod(type);

            var ignoreMethod = typeof(EntityFrameworkQueryableExtensions)
                .GetMethods()
                .Single(m =>
                    m.Name == nameof(EntityFrameworkQueryableExtensions.IgnoreQueryFilters) &&
                    m.IsGenericMethod &&
                    m.GetParameters().Length == 1)
                .MakeGenericMethod(type);

            var noTrackingMethod = typeof(EntityFrameworkQueryableExtensions)
                .GetMethods()
                .Single(m =>
                    m.Name == nameof(EntityFrameworkQueryableExtensions.AsNoTracking) &&
                    m.IsGenericMethod &&
                    m.GetParameters().Length == 1)
                .MakeGenericMethod(type);

            return ctx =>
            {
                var q = setMethod.Invoke(ctx, null)!;
                q = ignoreMethod.Invoke(null, [q])!;
                q = noTrackingMethod.Invoke(null, [q])!;

                return (IQueryable)q;
            };
        });

        return factory(context);
    }

    private static async Task<IList<TResultEntity>> GetResultsFromTable<TTargetDbContext, TResultEntity>(TTargetDbContext context, FormattableString queryString, CancellationToken cancellationToken)
        where TTargetDbContext : DbContext
    {
        var query = context.Database.SqlQuery<TResultEntity>(queryString);

        return await query.ToArrayAsync(cancellationToken);
    }

    private static List<IEntity> OrderHierarchically(List<IEntity> entities, Type entityClrType)
    {
        var parentProp = entityClrType.GetProperty("ParentId");

        if (parentProp is null)
        {
            return entities;
        }

        var lookup = entities.ToLookup(e =>
        {
            var pid = parentProp.GetValue(e);

            return pid is null ? Guid.Empty : (Guid)pid;
        });

        var result = new List<IEntity>();

        void Visit(IEntity entity)
        {
            result.Add(entity);

            var id = entity.Id;

            foreach (var child in lookup[id])
            {
                Visit(child);
            }
        }

        foreach (var root in lookup[Guid.Empty])
        {
            Visit(root);
        }

        return result;
    }
}
