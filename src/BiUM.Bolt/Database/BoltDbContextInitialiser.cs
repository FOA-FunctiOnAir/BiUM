using BiUM.Bolt.Database.Entities;
using BiUM.Core.Common.Configs;
using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Common.Utils;
using BiUM.Specialized.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;

namespace BiUM.Bolt.Database;

public partial class BoltDbContextInitialiser<TBoltDbContext, TDbContext> : DbContextInitialiser<TDbContext>, IBaseBoltDbContextInitialiser
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
            _boltContext.Database.EnsureCreated();

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

        if (_boltOptions == null || !_boltOptions.Enable) return;

        BoltStatus boltStatus = null;
        BoltTransaction lastTransaction = null;

        var boltStatuses = await GetResultsFromTable<TDbContext, BoltStatus>(_context, $"SELECT * FROM dbo.\"__BOLT_STATUS\"", cancellationToken);

        if (boltStatuses != null && boltStatuses.Any())
        {
            boltStatus = boltStatuses[0];
        }

        if (boltStatus != null && boltStatus.LastTransactionId != null)
        {
            var boltTransactions = await GetResultsFromTable<TBoltDbContext, BoltTransaction>(
                _boltContext,
                FormattableStringFactory.Create($"SELECT * FROM dbo.\"__BOLT_TRANSACTION\" WHERE \"ID\" = '{boltStatus.LastTransactionId.ToString()}'"),
                cancellationToken);

            if (boltTransactions != null && boltTransactions.Any())
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

        //var dbTransaction = _context.Database.BeginTransaction();
        var isError = false;

        try
        {
            var grouppedTableTransactions = transactions.GroupBy(x => x.TableName);

            foreach (var transaction in transactions)
            {
                var ids = transaction.Ids?.Trim().Split(";");
                var guidIds = ids?.Select(x => new Guid(x.Trim()));
                var allIds = guidIds ?? [];

                var uniqueIds = allIds.Distinct();
                if (!uniqueIds.Any()) continue;

                var linqBoltQuery = _boltContext.GetType().GetProperty(transaction.TableName)?.GetValue(_boltContext) as IQueryable<IEntity>;
                if (linqBoltQuery is null) continue;

                var boltEntities = await linqBoltQuery.Where(x => uniqueIds.Contains(x.Id)).ToListAsync(cancellationToken);

                var linqTargetQuery = _context.GetType().GetProperty(transaction.TableName)?.GetValue(_context) as IQueryable<IEntity>;
                if (linqTargetQuery is null) continue;

                var targetEntities = await linqTargetQuery.AsNoTracking().Where(x => uniqueIds.Contains(x.Id)).ToListAsync(cancellationToken);

                if (transaction.Delete)
                {
                    var deleteEntities = boltEntities.Where(x => targetEntities.Select(x => x.Id).Contains(x.Id));

                    if (deleteEntities.Any())
                    {
                        foreach (var entity in deleteEntities)
                        {
                            if (entity is IBaseEntity)
                            {
                                (entity as IBaseEntity).Deleted = true;
                            }
                        }

                        _context.UpdateRange(deleteEntities);
                    }
                }
                else
                {
                    var insertEntities = boltEntities.Where(x => !targetEntities.Select(x => x.Id).Contains(x.Id));
                    var updateEntities = boltEntities.Where(x => targetEntities.Select(x => x.Id).Contains(x.Id));

                    if (insertEntities.Any())
                    {
                        _context.AddRange(insertEntities);
                    }

                    if (updateEntities.Any())
                    {
                        _context.UpdateRange(updateEntities);
                    }
                }

                await _context.SaveChangesAsync(cancellationToken);
            }

            //foreach (var tableTransactions in grouppedTableTransactions)
            //{
            //    var allIds = new List<Guid>();

            //    foreach (var transaction in tableTransactions)
            //    {
            //        var ids = transaction.Ids?.Trim().Split(";");
            //        var guidIds = ids?.Select(x => new Guid(x.Trim()));
            //        allIds.AddRange(guidIds ?? []);
            //    }

            //    var uniqueIds = allIds.Distinct();
            //    if (!uniqueIds.Any()) continue;

            //    var linqQuery = _boltContext.GetType().GetProperty(tableTransactions.Key)?.GetValue(_boltContext) as IQueryable<IBaseEntity>;
            //    if (linqQuery is null) continue;

            //    var entities = await linqQuery.Where(x => uniqueIds.Contains(x.Id)).ToListAsync(cancellationToken);

            //    var linqTargetQuery = _context.GetType().GetProperty(tableTransactions.Key)?.GetValue(_context) as IQueryable<IBaseEntity>;
            //    if (linqTargetQuery is null) continue;

            //    var targetEntities = await linqTargetQuery.AsNoTracking().Where(x => uniqueIds.Contains(x.Id)).ToListAsync(cancellationToken);

            //    var insertEntities = entities.Where(x => !targetEntities.Select(x => x.Id).Contains(x.Id));
            //    var updateEntities = entities.Where(x => targetEntities.Select(x => x.Id).Contains(x.Id));

            //    if (insertEntities.Any())
            //    {
            //        _context.AddRange(insertEntities);
            //    }
            //    if (updateEntities.Any())
            //    {
            //        _context.UpdateRange(updateEntities);
            //    }

            //    await _context.SaveChangesAsync(cancellationToken);
            //}
        }
        catch (Exception ex)
        {
            isError = true;

            if (boltStatus == null)
            {
                boltStatus = new BoltStatus()
                {
                    Id = boltStatusId,
                    LastTransactionId = null,
                    Error = ex.Message
                };

                _context.Add(boltStatus);
            }
            else
            {
                boltStatus.Error = ex.Message;

                _context.Update(boltStatus);
            }

            _context.SaveChanges();
        }
        finally
        {
            if (!isError)
            {
                var last = transactions.Last();

                if (boltStatus == null)
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

                    _context.Update(boltStatus);
                }

                _context.SaveChanges();
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