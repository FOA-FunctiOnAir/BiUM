using BiUM.Contract.Models;
using BiUM.Specialized.Database;
using System.Collections.Generic;

namespace BiUM.Specialized.Services.DynamicApi;

public interface IDynamicApiExecutionContext
{
    IDbContext Db { get; }

    IReadOnlyDictionary<string, object?> Parameters { get; }

    int? PageStart { get; }

    int? PageSize { get; }

    CorrelationContext? Correlation { get; }

    string ConnectionString { get; }

    string DatabaseType { get; }
}