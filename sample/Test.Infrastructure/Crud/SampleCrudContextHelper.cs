using BiApp.Test.Infrastructure.DynamicApi;
using BiUM.Contract.Models;
using BiUM.Core.Authorization;
using BiUM.Specialized.Services.Crud;
using System;

namespace BiApp.Test.Infrastructure.Crud;

public static class SampleCrudContextHelper
{
    public readonly record struct SampleContext(Guid ApplicationId, Guid TenantId, string Schema);

    public static SampleContext Resolve(ICorrelationContextProvider provider)
    {
        var correlation = provider.Get();
        var applicationId = correlation?.ApplicationId is { } app && app != Guid.Empty
            ? app
            : SampleDynamicApiConstants.ApplicationId;
        var tenantId = correlation?.TenantId is { } tenant && tenant != Guid.Empty
            ? tenant
            : SampleCrudConstants.TenantId;

        return new SampleContext(
            applicationId,
            tenantId,
            CrudSchemaHelper.ResolveSchema(applicationId, tenantId));
    }

    public static SampleContext Ensure(ICorrelationContextAccessor accessor, ICorrelationContextProvider provider)
    {
        var resolved = Resolve(provider);
        var current = provider.Get();
        var needsPatch = current is null
            || !current.TenantId.HasValue
            || current.TenantId.Value == Guid.Empty
            || current.ApplicationId == Guid.Empty;

        if (needsPatch)
        {
            accessor.CorrelationContext = new CorrelationContext
            {
                CorrelationId = current?.CorrelationId ?? Guid.NewGuid(),
                ApplicationId = resolved.ApplicationId,
                TenantId = resolved.TenantId,
                LanguageId = current?.LanguageId ?? CorrelationContext.DefaultLanguageId,
                TraceId = current?.TraceId ?? "sample-test",
                ConnectionId = current?.ConnectionId ?? "sample-test",
                ClientHost = current?.ClientHost ?? "localhost"
            };
        }

        return Resolve(provider);
    }
}