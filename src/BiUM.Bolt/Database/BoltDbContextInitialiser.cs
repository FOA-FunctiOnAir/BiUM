using BiUM.Bolt.Database.Entities;
using BiUM.Core.Common.Configs;
using BiUM.Core.Common.Utils;
using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
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
    public readonly BoltOptions _boltOptions;
    public readonly TBoltDbContext _boltContext;

    public BoltDbContextInitialiser(ILogger<BoltDbContextInitialiser<TBoltDbContext, TDbContext>> logger, IOptions<BoltOptions> boltOptions, TBoltDbContext boltContext, TDbContext context)
        : base(logger, context)
    {
        _boltOptions = boltOptions.Value;
        _boltContext = boltContext;
    }

    public virtual async Task InitialiseAsync()
    {
        try
        {
            await _boltContext.Database.EnsureCreatedAsync();

            if (_boltContext.Database.IsSqlServer())
            {
                await _boltContext.Database.MigrateAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initialising the bolt database.");

            throw;
        }
    }

    public virtual async Task EqualizeAsync(CancellationToken cancellationToken = default)
    {
        var boltStatusId = GuidGenerator.NewGuid("Bolt-App-Status-Id");

        if (!_boltOptions.Enable)
        {
            return;
        }

        BoltStatus boltStatus = null;
        BoltTransaction lastTransaction = null;

        var boltStatuses = await GetResultsFromTable<TDbContext, BoltStatus>(_context, $"SELECT * FROM dbo.\"__BOLT_STATUS\"", cancellationToken);

        if (boltStatuses.Any())
        {
            boltStatus = boltStatuses[0];
        }

        if (boltStatus?.LastTransactionId is not null)
        {
            var boltTransactions = await GetResultsFromTable<TBoltDbContext, BoltTransaction>(
                _boltContext,
                FormattableStringFactory.Create($"SELECT * FROM dbo.\"__BOLT_TRANSACTION\" WHERE \"ID\" = '{boltStatus.LastTransactionId.ToString()}'"),
                cancellationToken);

            if (boltTransactions.Any())
            {
                lastTransaction = boltTransactions[0];
            }
        }

        var queryStatement = lastTransaction is null ? string.Empty : $" WHERE \"ID\" != '{lastTransaction.Id.ToString()}' AND (\"CREATED\" > '{lastTransaction.Created.ToString("yyyy-MM-dd")}' or (\"CREATED\" = '{lastTransaction.Created.ToString("yyyy-MM-dd")}' and \"CREATED_TIME\" > '{lastTransaction.CreatedTime.ToString("HH:mm:ss")}'))";
        var query = $"SELECT * FROM dbo.\"__BOLT_TRANSACTION\"" + (string.IsNullOrEmpty(queryStatement) ? "" : queryStatement) + " ORDER BY \"CREATED\" ASC, \"SORT_ORDER\" ASC, \"CREATED_TIME\" ASC";

        var transactions = await GetResultsFromTable<TBoltDbContext, BoltTransaction>(
            _boltContext,
            FormattableStringFactory.Create(query),
            cancellationToken);

        if (!transactions.Any()) { return; }

        var isError = false;
        var transactionId = Guid.Empty;
        Guid? lastTransactionId = null;

        try
        {
            foreach (var transaction in transactions)
            {
                transactionId = transaction.Id;

                var ids = transaction.Ids?.Trim().Split(";");
                var guidIds = ids?.Select(x => new Guid(x.Trim()));
                var allIds = guidIds ?? [];

                var uniqueIds = allIds.Distinct().ToArray();

                if (uniqueIds.Length == 0) continue;

                if (_boltContext.GetType().GetProperty(transaction.TableName)?.GetValue(_boltContext) is not IQueryable<IEntity> linqBoltQuery) continue;

                var boltEntities = await linqBoltQuery.AsNoTracking().Where(x => uniqueIds.Contains(x.Id)).ToListAsync(cancellationToken);

                if (_context.GetType().GetProperty(transaction.TableName)?.GetValue(_context) is not IQueryable<IEntity> linqTargetQuery) continue;

                var targetEntities = await linqTargetQuery.AsNoTracking().Where(x => uniqueIds.Contains(x.Id)).ToListAsync(cancellationToken);

                if (transaction.Delete)
                {
                    var deleteEntities = boltEntities.Where(b => targetEntities.Any(t => t.Id == b.Id));

                    _context.RemoveRange(deleteEntities);
                }
                else
                {
                    var insertEntities = boltEntities.Where(b => targetEntities.All(t => t.Id != b.Id));
                    var updateEntities = boltEntities.Where(b => targetEntities.Any(t => t.Id == b.Id));

                    _context.AddRange(insertEntities);
                    _context.UpdateRange(updateEntities);
                }

                await _context.SaveChangesAsync(cancellationToken);

                lastTransactionId = transactionId;

                _context.ChangeTracker.Clear();
            }
        }
        catch (Exception ex)
        {
            isError = true;

            _context.ChangeTracker.Clear();

            if (boltStatus is null)
            {
                boltStatus = new BoltStatus()
                {
                    Id = boltStatusId,
                    LastTransactionId = lastTransactionId,
                    Error = $"TransactionId:{transactionId}, Message:{ex.GetFullMessage()}"
                };

                _context.Add(boltStatus);
            }
            else
            {
                boltStatus.LastTransactionId = lastTransactionId ?? boltStatus.LastTransactionId;
                boltStatus.Error = $"TransactionId:{transactionId}, Message:{ex.GetFullMessage()}";

                _context.Update(boltStatus);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            if (!isError)
            {
                var last = transactions
                    .OrderByDescending(x => x.Created)
                    .ThenByDescending(x => x.CreatedTime)
                    .ThenByDescending(x => x.SortOrder)
                    .First();

                if (boltStatus is null)
                {
                    boltStatus = new BoltStatus()
                    {
                        Id = boltStatusId,
                        LastTransactionId = last.Id,
                        Error = null
                    };

                    _context.Add(boltStatus);
                }
                else
                {
                    boltStatus.LastTransactionId = last.Id;
                    boltStatus.Error = null;

                    _context.Update(boltStatus);
                }

                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }

    public async Task<IList<TResultEntity>> GetResultsFromTable<TTargetDbContext, TResultEntity>(TTargetDbContext context, FormattableString queryString, CancellationToken cancellationToken)
        where TTargetDbContext : DbContext
    {
        var query = context.Database.SqlQuery<TResultEntity>(queryString);

        return await query.ToArrayAsync(cancellationToken);
    }
}