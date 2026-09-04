using BiUM.Contract.Models.Api;
using BiUM.Core.Common.Utils;
using BiUM.Core.Constants;
using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Common.DynamicApi;
using BiUM.Specialized.Common.Models;
using BiUM.Specialized.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Services.DynamicApi;

public partial class DynamicApiService
{
    public virtual async Task<ApiResponse> SaveDomainDynamicApiAsync(
        SaveDomainDynamicApiCommand command,
        CancellationToken cancellationToken)
    {
        var response = new ApiResponse();

        if (command.ApplicationId == Guid.Empty)
        {
            await AddMessage(response, "dynamic_api_application_is_required", cancellationToken);
            return response;
        }

        if (command.MicroserviceId == Guid.Empty)
        {
            await AddMessage(response, "dynamic_api_microservice_is_required", cancellationToken);
            return response;
        }

        if (string.IsNullOrWhiteSpace(command.Code))
        {
            await AddMessage(response, "dynamic_api_code_is_required", cancellationToken);
            return response;
        }

        var domainDynamicApi = await DbContext.DomainDynamicApis
            .Include(x => x.DynamicApiParameters)
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);

        if (domainDynamicApi is null)
        {
            if (!CanCreateDefinition())
            {
                await AddMessage(response, "dynamic_api_definition_access_denied", cancellationToken);
                return response;
            }

            domainDynamicApi = new DomainDynamicApi
            {
                Id = command.Id ?? GuidGenerator.New(),
                ApplicationId = command.ApplicationId,
                MicroserviceId = command.MicroserviceId,
                Name = command.NameTr!.ToTranslationString(),
                Code = command.Code.Trim(),
                HttpType = command.HttpType,
                ExecutionType = command.ExecutionType,
                RuntimePlatformType = command.RuntimePlatformType,
                SourceCode = command.SourceCode,
                Compensatible = command.Compensatible,
                CompileStatusType = Ids.Parameter.DynamicApiCompileStatus.Values.Draft
            };

            _ = DbContext.DomainDynamicApis.Add(domainDynamicApi);

            foreach (var param in command.DynamicApiParameters.Where(p => p._rowStatus != RowStatuses.Deleted))
            {
                DbContext.DomainDynamicApiParameters.Add(new DomainDynamicApiParameter
                {
                    DynamicApiId = domainDynamicApi.Id,
                    DirectionType = param.DirectionType,
                    Property = param.Property,
                    FieldId = param.FieldId
                });
            }
        }
        else
        {
            if (!CanMutateDefinition(domainDynamicApi))
            {
                await AddMessage(response, "dynamic_api_definition_access_denied", cancellationToken);
                return response;
            }

            domainDynamicApi.Name = command.NameTr!.ToTranslationString();
            domainDynamicApi.Code = command.Code.Trim();
            domainDynamicApi.HttpType = command.HttpType;
            domainDynamicApi.ExecutionType = command.ExecutionType;
            domainDynamicApi.RuntimePlatformType = command.RuntimePlatformType;
            domainDynamicApi.SourceCode = command.SourceCode;
            domainDynamicApi.Compensatible = command.Compensatible;
            domainDynamicApi.CompileStatusType = Ids.Parameter.DynamicApiCompileStatus.Values.Draft;
            domainDynamicApi.CompileError = null;

            var paramIds = command.DynamicApiParameters
                .Where(p => p._rowStatus is RowStatuses.Edited or RowStatuses.Deleted)
                .Select(p => p.Id)
                .Distinct()
                .ToList();

            var prefetched = paramIds.Count > 0
                ? await DbContext.DomainDynamicApiParameters
                    .Where(p => paramIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id, cancellationToken)
                : [];

            foreach (var param in command.DynamicApiParameters)
            {
                switch (param._rowStatus)
                {
                    case RowStatuses.New:
                        DbContext.DomainDynamicApiParameters.Add(new DomainDynamicApiParameter
                        {
                            DynamicApiId = domainDynamicApi.Id,
                            DirectionType = param.DirectionType,
                            Property = param.Property,
                            FieldId = param.FieldId
                        });
                        break;
                    case RowStatuses.Edited when prefetched.TryGetValue(param.Id, out var existing):
                        existing.DirectionType = param.DirectionType;
                        existing.Property = param.Property;
                        existing.FieldId = param.FieldId;
                        _ = DbContext.DomainDynamicApiParameters.Update(existing);
                        break;
                    case RowStatuses.Deleted when prefetched.TryGetValue(param.Id, out var toDelete):
                        _ = DbContext.DomainDynamicApiParameters.Remove(toDelete);
                        break;
                }
            }

            _ = DbContext.DomainDynamicApis.Update(domainDynamicApi);
        }

        await SaveTranslations(
            DbContext.DomainDynamicApiTranslations,
            domainDynamicApi.Id,
            nameof(domainDynamicApi.Name),
            command.NameTr ?? [],
            cancellationToken);

        if (!await ValidateAndSyncTableReferencesAsync(
                domainDynamicApi,
                command.ApplicationId,
                command.MicroserviceId,
                command.SourceCode,
                requirePhysicalTable: false,
                response,
                cancellationToken))
        {
            return response;
        }

        _ = await DbContext.SaveChangesAsync(cancellationToken);

        return response;
    }

    public virtual async Task<ApiResponse> DeleteDomainDynamicApiAsync(Guid id, CancellationToken cancellationToken)
    {
        var response = new ApiResponse();

        var domainDynamicApi = await DbContext.DomainDynamicApis
            .Include(x => x.DynamicApiParameters)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (domainDynamicApi is null)
        {
            await AddMessage(response, "dynamic_api_definition_not_found", cancellationToken);
            return response;
        }

        if (!CanMutateDefinition(domainDynamicApi))
        {
            await AddMessage(response, "dynamic_api_definition_access_denied", cancellationToken);
            return response;
        }

        var hasVersions = await DbContext.DomainDynamicApiVersions.AnyAsync(x => x.DynamicApiId == id, cancellationToken);

        if (hasVersions)
        {
            await AddMessage(response, "dynamic_api_definition_can_not_delete_that_published", cancellationToken);
            return response;
        }

        foreach (var param in domainDynamicApi.DynamicApiParameters.ToList())
        {
            _ = DbContext.DomainDynamicApiParameters.Remove(param);
        }

        foreach (var table in await DbContext.DomainDynamicApiTables.Where(t => t.DynamicApiId == id).ToListAsync(cancellationToken))
        {
            _ = DbContext.DomainDynamicApiTables.Remove(table);
        }

        _ = DbContext.DomainDynamicApis.Remove(domainDynamicApi);
        _ = await DbContext.SaveChangesAsync(cancellationToken);

        return response;
    }

    public virtual async Task<ApiResponse<DomainDynamicApiDto>> GetDomainDynamicApiAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var returnObject = new ApiResponse<DomainDynamicApiDto>();

        var domainDynamicApi = await DbContext.DomainDynamicApis
            .Include(x => x.DynamicApiParameters)
            .Include(x => x.DomainDynamicApiTranslations.Where(y => y.LanguageId == CorrelationContext.LanguageId))
            .Where(ReadFilter())
            .FirstOrDefaultAsync<DomainDynamicApi, DomainDynamicApiDto>(x => x.Id == id, Mapper, cancellationToken);

        returnObject.Value = domainDynamicApi;

        return returnObject;
    }

    public virtual async Task<ApiResponse<DomainDynamicApiDto>> GetDomainDynamicApiByCodeAsync(
        string code,
        CancellationToken cancellationToken)
    {
        var returnObject = new ApiResponse<DomainDynamicApiDto>();

        var domainDynamicApi = await DbContext.DomainDynamicApis
            .Include(x => x.DynamicApiParameters)
            .Include(x => x.DomainDynamicApiTranslations.Where(y => y.LanguageId == CorrelationContext.LanguageId))
            .Where(ReadFilter())
            .FirstOrDefaultAsync<DomainDynamicApi, DomainDynamicApiDto>(x => x.Code == code, Mapper, cancellationToken);

        returnObject.Value = domainDynamicApi;

        return returnObject;
    }

    public virtual async Task<PaginatedApiResponse<DomainDynamicApisDto>> GetDomainDynamicApisAsync(
        Guid? applicationId,
        string? name,
        string? code,
        string? q,
        int? pageStart,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        return await DbContext.DomainDynamicApis
            .Include(x => x.DomainDynamicApiTranslations.Where(y => y.LanguageId == CorrelationContext.LanguageId))
            .Where(ReadFilter())
            .Where(api =>
                (!applicationId.HasValue || api.ApplicationId == applicationId.Value) &&
                (string.IsNullOrEmpty(q) || api.DomainDynamicApiTranslations.Any(rt => rt.Translation != null && rt.LanguageId == CorrelationContext.LanguageId && rt.Translation.ToLower().Contains(q.ToLower()))) &&
                (string.IsNullOrEmpty(name) || (!string.IsNullOrEmpty(api.Name) && api.Name.Contains(name, StringComparison.CurrentCultureIgnoreCase))) &&
                (string.IsNullOrEmpty(code) || (!string.IsNullOrEmpty(api.Code) && api.Code.Contains(code, StringComparison.CurrentCultureIgnoreCase))))
            .ToPaginatedListAsync<DomainDynamicApi, DomainDynamicApisDto>(
                PaginationQuery.ToPageBaseQuery(pageStart, pageSize),
                Mapper,
                cancellationToken);
    }

    public async Task<bool> IsDynamicApiMutationCompensatibleByCodeAsync(string code, CancellationToken cancellationToken)
    {
        try
        {
            var definition = await DbContext.DomainDynamicApis
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Code == code, cancellationToken);

            return definition?.Compensatible == true;
        }
        catch
        {
            return false;
        }
    }

    private Expression<Func<DomainDynamicApi, bool>> ReadFilter()
    {
        var ctxTenant = CorrelationContext.TenantId;

        return api =>
            api.TenantId == Ids.Customer.System.Id ||
            ctxTenant == null ||
            (ctxTenant.HasValue && api.TenantId == ctxTenant.Value);
    }

    private static bool IsSystemTenantContext(Guid? ctxTenant) =>
        ctxTenant.HasValue && ctxTenant.Value == Ids.Customer.System.Id;

    private bool CanMutateDefinition(DomainDynamicApi api)
    {
        var ctx = CorrelationContext.TenantId;

        if (IsSystemTenantContext(ctx) || ctx is null)
        {
            return true;
        }

        return ctx.HasValue && api.TenantId == ctx.Value;
    }

    private bool CanCreateDefinition()
    {
        var ctx = CorrelationContext.TenantId;

        if (IsSystemTenantContext(ctx) || ctx is null)
        {
            return true;
        }

        return ctx.HasValue && ctx.Value != Guid.Empty;
    }
}