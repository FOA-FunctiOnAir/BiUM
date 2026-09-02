using BiUM.Contract.Models.Api;
using BiUM.Infrastructure.Common.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Services.DynamicApi;

public partial class DynamicApiService
{
    public async Task<object> ExecuteAsync(
        string code,
        Guid expectedHttpType,
        Dictionary<string, object?> parameters,
        int? pageStart,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        var version = await GetLatestVersionByCodeAsync(code, cancellationToken);

        if (version.HttpType != expectedHttpType)
        {
            throw new InvalidOperationException("dynamic_api_http_method_mismatch");
        }

        if (version.CompiledAssembly is null || string.IsNullOrEmpty(version.EntryPointTypeName))
        {
            throw new InvalidOperationException("dynamic_api_not_published");
        }

        var cacheKey = BuildCacheKey(code, version.Version);
        var handler = _runtimeCache.GetOrLoad(cacheKey, version.CompiledAssembly, version.EntryPointTypeName);

        var context = new DynamicApiExecutionContext(
            DbContext,
            parameters,
            pageStart,
            pageSize,
            CorrelationContext,
            DbContext.Database.GetConnectionString() ?? string.Empty,
            _dbType);

        var result = await handler.ExecuteAsync(context, cancellationToken);

        if (result is ApiResponse)
        {
            return result;
        }

        throw new InvalidOperationException("dynamic_api_invalid_response_type");
    }

    private async Task<DomainDynamicApiVersion> GetLatestVersionByCodeAsync(string code, CancellationToken cancellationToken)
    {
        var version = await DbContext.DomainDynamicApiVersions
            .Include(v => v.DynamicApi)
            .Where(v => v.Code == code)
            .OrderByDescending(v => v.Version)
            .FirstOrDefaultAsync(cancellationToken);

        if (version is null)
        {
            throw new InvalidOperationException("dynamic_api_not_found");
        }

        return version;
    }
}