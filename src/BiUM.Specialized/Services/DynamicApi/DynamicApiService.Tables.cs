using BiUM.Contract.Models.Api;
using BiUM.Core.Constants;
using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Common.DynamicApi;
using BiUM.Specialized.Services.Crud;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Services.DynamicApi;

public partial class DynamicApiService
{
    public async Task<ApiResponse<IReadOnlyList<DynamicApiSelectableTableDto>>> GetDynamicApiSelectableTablesAsync(
        Guid applicationId,
        Guid microserviceId,
        CancellationToken cancellationToken)
    {
        var response = new ApiResponse<IReadOnlyList<DynamicApiSelectableTableDto>> { Value = [] };

        if (applicationId == Guid.Empty || microserviceId == Guid.Empty)
        {
            await AddMessage(response, "dynamic_api_application_or_microservice_is_required", cancellationToken);
            return response;
        }

        var result = new Dictionary<string, DynamicApiSelectableTableDto>(StringComparer.OrdinalIgnoreCase);

        var cruds = await DbContext.DomainCruds
            .AsNoTracking()
            .Where(c => c.MicroserviceId == microserviceId && c.ApplicationId == applicationId)
            .Where(DomainCrudReadFilter())
            .Select(c => new
            {
                c.Id,
                c.Code,
                c.Name,
                c.TableName,
                c.ApplicationId,
                c.TenantId
            })
            .ToListAsync(cancellationToken);

        var publishedCrudIds = await DbContext.DomainCrudVersions
            .AsNoTracking()
            .Where(v => cruds.Select(c => c.Id).Contains(v.CrudId))
            .Select(v => v.CrudId)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var crud in cruds)
        {
            var schema = CrudSchemaHelper.ResolveSchema(crud.ApplicationId, crud.TenantId);
            var key = $"{schema}.{crud.TableName}";

            result[key] = new DynamicApiSelectableTableDto
            {
                Schema = schema,
                TableName = crud.TableName,
                Source = DynamicApiTableSource.Crud,
                DisplayName = crud.Name,
                CrudCode = crud.Code,
                CrudId = crud.Id,
                ApplicationId = crud.ApplicationId,
                TenantId = crud.TenantId,
                IsPublished = publishedCrudIds.Contains(crud.Id)
            };
        }

        var introspector = await CreateIntrospectorAsync(cancellationToken);

        if (introspector is not null)
        {
            var catalogTables = await introspector.ListCatalogTablesAsync(cancellationToken);

            foreach (var table in catalogTables)
            {
                var key = table.Key;

                if (result.ContainsKey(key))
                {
                    continue;
                }

                result[key] = new DynamicApiSelectableTableDto
                {
                    Schema = table.Schema,
                    TableName = table.TableName,
                    Source = DynamicApiTableSource.Catalog,
                    DisplayName = table.TableName,
                    IsPublished = true
                };
            }
        }

        response.Value = result.Values
            .OrderBy(t => t.Source)
            .ThenBy(t => t.Schema)
            .ThenBy(t => t.TableName)
            .ToList();

        return response;
    }

    public async Task<ApiResponse<IReadOnlyList<DomainDynamicApisByTableDto>>> GetDomainDynamicApisByTableAsync(
        string schema,
        string tableName,
        CancellationToken cancellationToken)
    {
        var response = new ApiResponse<IReadOnlyList<DomainDynamicApisByTableDto>> { Value = [] };

        if (string.IsNullOrWhiteSpace(schema) || string.IsNullOrWhiteSpace(tableName))
        {
            await AddMessage(response, "dynamic_api_table_name_invalid", cancellationToken);
            return response;
        }

        response.Value = await DbContext.DomainDynamicApiTables
            .AsNoTracking()
            .Where(t => t.Schema == schema && t.TableName == tableName)
            .Join(
                DbContext.DomainDynamicApis.AsNoTracking().Where(ReadFilter()),
                table => table.DynamicApiId,
                api => api.Id,
                (table, api) => new DomainDynamicApisByTableDto
                {
                    Id = api.Id,
                    Code = api.Code,
                    Name = api.Name,
                    ApplicationId = api.ApplicationId,
                    TenantId = api.TenantId,
                    MicroserviceId = api.MicroserviceId,
                    CompileStatusType = api.CompileStatusType
                })
            .Distinct()
            .OrderBy(x => x.Code)
            .ToListAsync(cancellationToken);

        return response;
    }

    private System.Linq.Expressions.Expression<Func<DomainCrud, bool>> DomainCrudReadFilter()
    {
        var ctxTenant = CorrelationContext.TenantId;

        return crud =>
            crud.TenantId == Ids.Customer.System.Id ||
            ctxTenant == null ||
            (ctxTenant.HasValue && crud.TenantId == ctxTenant.Value);
    }

    private async Task<IDynamicApiTableIntrospector?> CreateIntrospectorAsync(CancellationToken cancellationToken)
    {
        var connection = DbContext.Database.GetDbConnection();

        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        return DynamicApiTableIntrospectorFactory.Create(_dbType, connection);
    }

    private async Task<bool> ValidateAndSyncTableReferencesAsync(
        DomainDynamicApi domainDynamicApi,
        Guid applicationId,
        Guid microserviceId,
        string sourceCode,
        bool requirePhysicalTable,
        ApiResponse response,
        CancellationToken cancellationToken)
    {
        var parseResult = DynamicApiEntityParser.Parse(sourceCode);

        foreach (var error in parseResult.Errors)
        {
            await AddMessage(response, error, cancellationToken);
        }

        if (!response.Success && parseResult.Errors.Count > 0)
        {
            return false;
        }

        IDynamicApiTableIntrospector? introspector = null;

        if (requirePhysicalTable)
        {
            introspector = await CreateIntrospectorAsync(cancellationToken);
        }

        var validation = await _tableReferenceValidator.ValidateAsync(
            DbContext,
            _dbType,
            applicationId,
            microserviceId,
            domainDynamicApi.TenantId,
            CorrelationContext.TenantId,
            parseResult.TableReferences,
            requirePhysicalTable,
            introspector,
            cancellationToken);

        foreach (var error in validation.Errors.Distinct())
        {
            await AddMessage(response, error, cancellationToken);
        }

        if (!validation.Success)
        {
            return false;
        }

        await SyncDynamicApiTablesAsync(domainDynamicApi.Id, parseResult.TableReferences, cancellationToken);

        return true;
    }

    private async Task SyncDynamicApiTablesAsync(
        Guid dynamicApiId,
        IReadOnlyList<DynamicApiTableReference> references,
        CancellationToken cancellationToken)
    {
        var existing = await DbContext.DomainDynamicApiTables
            .Where(t => t.DynamicApiId == dynamicApiId)
            .ToListAsync(cancellationToken);

        var desiredKeys = references.Select(r => r.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var row in existing.Where(r => !desiredKeys.Contains(ToTableKey(r))))
        {
            _ = DbContext.DomainDynamicApiTables.Remove(row);
        }

        var existingKeys = existing.Select(ToTableKey).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var reference in references)
        {
            if (existingKeys.Contains(reference.Key))
            {
                continue;
            }

            DbContext.DomainDynamicApiTables.Add(new DomainDynamicApiTable
            {
                DynamicApiId = dynamicApiId,
                Schema = reference.Schema,
                TableName = reference.TableName
            });
        }
    }

    private static string ToTableKey(DomainDynamicApiTable table) => $"{table.Schema}.{table.TableName}";
}