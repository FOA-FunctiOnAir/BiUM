using BiUM.Contract.Models;
using BiUM.Specialized.Database;
using System;
using System.Collections.Generic;

namespace BiUM.Specialized.Services.DynamicApi;

public sealed class DynamicApiExecutionContext : IDynamicApiExecutionContext, IDynamicApiRuntimeInternals
{
    private readonly Guid _dynamicApiId;
    private readonly string _connectionString;
    private readonly string _databaseType;

    public DynamicApiExecutionContext(
        IDbContext db,
        IReadOnlyDictionary<string, object?> parameters,
        int? pageStart,
        int? pageSize,
        CorrelationContext? correlation,
        Guid dynamicApiId,
        IDynamicApiHttp http,
        IDynamicApiCache cache,
        IDynamicApiMemoryCache memoryCache,
        IDynamicApiEvents events,
        string connectionString,
        string databaseType)
    {
        Db = db;
        Parameters = parameters;
        PageStart = pageStart;
        PageSize = pageSize;
        Correlation = correlation;
        _dynamicApiId = dynamicApiId;
        Http = http;
        Cache = cache;
        MemoryCache = memoryCache;
        Events = events;
        _connectionString = connectionString;
        _databaseType = databaseType;
    }

    public IDbContext Db { get; }

    public IReadOnlyDictionary<string, object?> Parameters { get; }

    public int? PageStart { get; }

    public int? PageSize { get; }

    public CorrelationContext? Correlation { get; }

    public IDynamicApiHttp Http { get; }

    public IDynamicApiCache Cache { get; }

    public IDynamicApiMemoryCache MemoryCache { get; }

    public IDynamicApiEvents Events { get; }

    Guid IDynamicApiRuntimeInternals.DynamicApiId => _dynamicApiId;

    string IDynamicApiRuntimeInternals.ConnectionString => _connectionString;

    string IDynamicApiRuntimeInternals.DatabaseType => _databaseType;

    internal Guid DynamicApiId => _dynamicApiId;

    internal string ConnectionString => _connectionString;

    internal string DatabaseType => _databaseType;
}