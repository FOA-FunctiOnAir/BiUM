using BiUM.Core.Constants;
using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Database;
using BiUM.Specialized.Services.Crud;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Services.DynamicApi;

public static class DynamicApiTableReferenceValidator
{
    public sealed class ValidationResult
    {
        public bool Success => Errors.Count == 0;

        public List<string> Errors { get; } = [];
    }

    public static async Task<ValidationResult> ValidateAsync(
        IDbContext dbContext,
        string databaseType,
        Guid applicationId,
        Guid microserviceId,
        Guid dynamicApiTenantId,
        Guid? correlationTenantId,
        IReadOnlyList<DynamicApiTableReference> references,
        bool requirePhysicalTable,
        IDynamicApiTableIntrospector? introspector,
        CancellationToken cancellationToken)
    {
        var result = new ValidationResult();

        foreach (var reference in references)
        {
            ValidateReferenceRules(
                databaseType,
                reference,
                correlationTenantId,
                result);

            if (!result.Success)
            {
                continue;
            }

            var matchedCrud = await FindCrudDefinitionAsync(
                dbContext,
                microserviceId,
                reference,
                cancellationToken);

            if (matchedCrud is not null)
            {
                if (matchedCrud.ApplicationId != applicationId)
                {
                    result.Errors.Add("dynamic_api_crud_table_tenant_mismatch");
                    continue;
                }

                if (matchedCrud.TenantId != dynamicApiTenantId)
                {
                    result.Errors.Add("dynamic_api_crud_table_tenant_mismatch");
                    continue;
                }

                if (matchedCrud.MicroserviceId != microserviceId)
                {
                    result.Errors.Add("dynamic_api_crud_table_tenant_mismatch");
                }
            }
            else if (DynamicApiSchemaRules.IsTenantCrudSchema(reference.Schema))
            {
                result.Errors.Add("dynamic_api_crud_table_not_found");
            }
            else if (IsCatalogSchema(reference.Schema, databaseType)
                     && !CanAccessCatalogSchema(correlationTenantId))
            {
                result.Errors.Add("dynamic_api_dbo_table_access_denied");
            }

            if (requirePhysicalTable && introspector is not null)
            {
                var exists = await introspector.TableExistsAsync(reference.Schema, reference.TableName, cancellationToken);

                if (!exists)
                {
                    result.Errors.Add("dynamic_api_table_not_exists_in_db");
                }
            }
        }

        return result;
    }

    private static void ValidateReferenceRules(
        string databaseType,
        DynamicApiTableReference reference,
        Guid? correlationTenantId,
        ValidationResult result)
    {
        if (!DynamicApiSchemaRules.IsAllowedSchema(reference.Schema, databaseType))
        {
            result.Errors.Add("dynamic_api_schema_not_allowed");
            return;
        }

        if (!DynamicApiSchemaRules.IsTableNameValid(reference.TableName))
        {
            result.Errors.Add("dynamic_api_table_name_invalid");
            return;
        }

        if (DynamicApiSchemaRules.IsTableBlocked(reference.Schema, reference.TableName))
        {
            result.Errors.Add("dynamic_api_table_not_allowed");
        }
    }

    private static bool IsCatalogSchema(string schema, string databaseType) =>
        string.Equals(schema, DynamicApiSchemaRules.GetCatalogSchema(databaseType), StringComparison.OrdinalIgnoreCase);

    private static bool CanAccessCatalogSchema(Guid? correlationTenantId) =>
        correlationTenantId is null
        || correlationTenantId.Value == Ids.Customer.System.Id;

    private static async Task<DomainCrud?> FindCrudDefinitionAsync(
        IDbContext dbContext,
        Guid microserviceId,
        DynamicApiTableReference reference,
        CancellationToken cancellationToken)
    {
        var cruds = await dbContext.DomainCruds
            .AsNoTracking()
            .Where(c => c.MicroserviceId == microserviceId)
            .ToListAsync(cancellationToken);

        return cruds.FirstOrDefault(c =>
            string.Equals(c.TableName, reference.TableName, StringComparison.OrdinalIgnoreCase)
            && string.Equals(CrudSchemaHelper.ResolveSchema(c.ApplicationId, c.TenantId), reference.Schema, StringComparison.OrdinalIgnoreCase));
    }
}