using BiUM.Contract.Models;
using BiUM.Specialized.Database;
using System.Collections.Generic;

namespace BiUM.Specialized.Services.DynamicApi;

public sealed class DynamicApiExecutionContext : IDynamicApiExecutionContext
{
    public DynamicApiExecutionContext(
        IDbContext db,
        IReadOnlyDictionary<string, object?> parameters,
        int? pageStart,
        int? pageSize,
        CorrelationContext? correlation,
        string connectionString,
        string databaseType)
    {
        Db = db;
        Parameters = parameters;
        PageStart = pageStart;
        PageSize = pageSize;
        Correlation = correlation;
        ConnectionString = connectionString;
        DatabaseType = databaseType;
    }

    public IDbContext Db { get; }

    public IReadOnlyDictionary<string, object?> Parameters { get; }

    public int? PageStart { get; }

    public int? PageSize { get; }

    public CorrelationContext? Correlation { get; }

    public string ConnectionString { get; }

    public string DatabaseType { get; }
}