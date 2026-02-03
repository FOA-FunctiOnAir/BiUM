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

public abstract class BoltDbContextInitialiser<TBoltDbContext, TDbContext> : DbContextInitialiser<TDbContext>, IBoltDbContextInitialiser
    where TBoltDbContext : DbContext
    where TDbContext : DbContext
{
    private static readonly ConcurrentDictionary<Type, Func<DbContext, IQueryable>> IgnoringFiltersQueryFactoryCache = new();

    protected TBoltDbContext BoltDbContext { get; }
    protected BoltOptions BoltOptions { get; }

    protected BoltDbContextInitialiser(IServiceProvider serviceProvider, IOptions<BoltOptions> boltOptions, TBoltDbContext boltDbContext, TDbContext dbContext)
        : base(dbContext, serviceProvider)
    {
        BoltOptions = boltOptions.Value;
        BoltDbContext = boltDbContext;
    }

    public override async Task InitialiseAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await BoltDbContext.Database.EnsureCreatedAsync(cancellationToken);

            if (BoltDbContext.Database.IsSqlServer())
            {
                await BoltDbContext.Database.MigrateAsync(cancellationToken);
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
        if (!BoltOptions.Enable)
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
                BoltDbContext,
                FormattableStringFactory.Create($"SELECT * FROM dbo.\"__BOLT_TRANSACTION\" WHERE \"ID\" = '{boltStatus.LastTransactionId}'"),
                cancellationToken);

            if (boltTransactions.Any())
            {
                lastTransaction = boltTransactions[0];
            }
        }

        var lastTransactionQueryStatement = lastTransaction is null ? string.Empty : $" WHERE \"ID\" != '{lastTransaction.Id}' AND (\"CREATED\" > '{lastTransaction.Created:yyyy-MM-dd}' or (\"CREATED\" = '{lastTransaction.Created:yyyy-MM-dd}' and \"CREATED_TIME\" > '{lastTransaction.CreatedTime:HH:mm:ss}'))";
        var lastTransactionQuery = "SELECT * FROM dbo.\"__BOLT_TRANSACTION\"" + (string.IsNullOrEmpty(lastTransactionQueryStatement) ? "" : lastTransactionQueryStatement) + " ORDER BY \"CREATED\" ASC, \"SORT_ORDER\" ASC, \"CREATED_TIME\" ASC";

        var allTransactions = await GetResultsFromTable<TBoltDbContext, BoltTransaction>(
            BoltDbContext,
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

                    var boltContextDbSetProp = BoltDbContext.GetType().GetProperty(transaction.TableName);

                    if (boltContextDbSetProp is null)
                    {
                        continue;
                    }

                    var boltEntityClrType = boltContextDbSetProp.PropertyType.GetGenericArguments()[0];

                    var boltQuery = GetQueryableIgnoringFilters(BoltDbContext, boltEntityClrType);

                    var boltEntities = await boltQuery
                        .Cast<IEntity>()
                        .Where(x => uniqueIds.AsEnumerable().Contains(x.Id))
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
                        .Where(x => uniqueIds.AsEnumerable().Contains(x.Id))
                        .ToListAsync(cancellationToken);

                    var entityType = BoltDbContext.Model.FindEntityType(boltEntityClrType);
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
                Error = $"CorrelationId: {lastCorrelationId}, TransactionId: {transactionId}\nMessage: {ex}"
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
        var factory = IgnoringFiltersQueryFactoryCache.GetOrAdd(entityClrType, type =>
        {
            var setMethod = typeof(DbContext)
                .GetMethods()
                .Single(m =>
                    m is { Name: nameof(Microsoft.EntityFrameworkCore.DbContext.Set), IsGenericMethod: true } &&
                    m.GetParameters().Length == 0)
                .MakeGenericMethod(type);

            var ignoreMethod = typeof(EntityFrameworkQueryableExtensions)
                .GetMethods()
                .Single(m =>
                    m is { Name: nameof(EntityFrameworkQueryableExtensions.IgnoreQueryFilters), IsGenericMethod: true } &&
                    m.GetParameters().Length == 1)
                .MakeGenericMethod(type);

            var noTrackingMethod = typeof(EntityFrameworkQueryableExtensions)
                .GetMethods()
                .Single(m =>
                    m is { Name: nameof(EntityFrameworkQueryableExtensions.AsNoTracking), IsGenericMethod: true } &&
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

        return factory.Invoke(context);
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

        foreach (var root in lookup[Guid.Empty])
        {
            Visit(root);
        }

        return result;

        void Visit(IEntity entity)
        {
            result.Add(entity);

            var id = entity.Id;

            foreach (var child in lookup[id])
            {
                Visit(child);
            }
        }
    }
}