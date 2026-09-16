using BiUM.Contract.Models;
using BiUM.Core.HttpClients;
using BiUM.Specialized.Database;
using BiUM.Specialized.Services.DynamicApi;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace BiUM.Tests.DynamicApi;

internal static class DynamicApiExecutionContextTestHelper
{
    internal static DynamicApiExecutionContext Create(
        IDbContext db,
        IReadOnlyDictionary<string, object?>? parameters = null,
        int? pageStart = null,
        int? pageSize = null,
        CorrelationContext? correlation = null,
        Guid? dynamicApiId = null,
        string connectionString = "Data Source=:memory:",
        string databaseType = DynamicApiSchemaRules.DbTypePostgresql,
        IMemoryCache? memoryCache = null)
    {
        var apiId = dynamicApiId ?? Guid.Empty;
        memoryCache ??= new MemoryCache(new MemoryCacheOptions());

        return new DynamicApiExecutionContext(
            db,
            parameters ?? new Dictionary<string, object?>(),
            pageStart,
            pageSize,
            correlation,
            apiId,
            new DynamicApiHttp(Mock.Of<IHttpClientsService>()),
            new DynamicApiCache(apiId, null),
            new DynamicApiMemoryCache(apiId, memoryCache),
            new DynamicApiEvents(new NoOpDynamicApiEventPublisher()),
            connectionString,
            databaseType);
    }
}