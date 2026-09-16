using BiUM.Contract.Models.Api;
using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Database;
using BiUM.Specialized.Services.DynamicApi;
using Microsoft.Extensions.Configuration;

namespace BiUM.Tests.Helpers;

internal sealed class TestNoOpCatalogDynamicApiService : DynamicApiService
{
    public TestNoOpCatalogDynamicApiService(
        IServiceProvider serviceProvider,
        IDbContext dbContext,
        DynamicApiRuntimeCache runtimeCache,
        IConfiguration configuration)
        : base(serviceProvider, dbContext, runtimeCache, configuration)
    {
    }

    protected override Task<ApiResponse> SaveDynamicApiServicesAsync(
        Guid applicationId,
        Guid microserviceId,
        string code,
        Guid httpType,
        IEnumerable<DomainDynamicApiParameter> parameters,
        CancellationToken cancellationToken) =>
        Task.FromResult(new ApiResponse());
}