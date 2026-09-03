using BiUM.Contract.Models.Api;
using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Database;
using BiUM.Specialized.Services.DynamicApi;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BiApp.Test.Infrastructure.DynamicApi;

public class SampleDynamicApiService : DynamicApiService
{
    public SampleDynamicApiService(
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