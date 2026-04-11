using BiUM.Contract.Models.Api;
using BiUM.Core.Common.Utils;
using BiUM.Core.Constants;
using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Common.Crud;
using BiUM.Specialized.Common.Models;
using BiUM.Specialized.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Services.Crud;

public partial class CrudService
{
    public virtual async Task<ApiResponse> PublishDomainCrudAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = new ApiResponse();

        var domainCrud = await DbContext.DomainCruds
            .Include(s => s.DomainCrudColumns)
            .Include(s => s.DomainCrudPartialUpdates)
                .ThenInclude(p => p.Columns)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (domainCrud is null)
        {
            await AddMessage(response, "crud_definition_not_found", cancellationToken);

            return response;
        }

        if (!CanMutateDomainCrud(domainCrud))
        {
            await AddMessage(response, "crud_definition_access_denied", cancellationToken);

            return response;
        }

        if (domainCrud.ApplicationId == Guid.Empty)
        {
            await AddMessage(response, "crud_application_is_required", cancellationToken);

            return response;
        }

        if (domainCrud.MicroserviceId == Guid.Empty)
        {
            await AddMessage(response, "crud_microservice_is_required", cancellationToken);

            return response;
        }

        if (string.IsNullOrEmpty(domainCrud.Code))
        {
            await AddMessage(response, "crud_code_is_required", cancellationToken);

            return response;
        }

        if (domainCrud.Code.Length < 3)
        {
            await AddMessage(response, "crud_code_should_be_min_char", cancellationToken);

            return response;
        }

        if (string.IsNullOrEmpty(domainCrud.TableName))
        {
            await AddMessage(response, "crud_table_name_is_required", cancellationToken);

            return response;
        }

        if (domainCrud.TableName.Length < 3)
        {
            await AddMessage(response, "crud_table_name_should_be_min_char", cancellationToken);

            return response;
        }

        if (domainCrud.DomainCrudColumns.Count == 0)
        {
            await AddMessage(response, "crud_should_have_columns", cancellationToken);

            return response;
        }

        if (domainCrud.DomainCrudColumns.Any(cc => cc.FieldId == Guid.Empty))
        {
            await AddMessage(response, "crud_columns_should_be_field", cancellationToken);

            return response;
        }

        if (domainCrud.DomainCrudColumns.Any(cc => cc.DataTypeId == Guid.Empty))
        {
            await AddMessage(response, "crud_columns_should_be_data_type", cancellationToken);

            return response;
        }

        var lastDomainCrudVersion = await DbContext.DomainCrudVersions
            .Include(s => s.DomainCrudVersionColumns)
            .OrderByDescending(s => s.Version)
            .FirstOrDefaultAsync(x => x.CrudId == domainCrud.Id, cancellationToken);

        var newVersion = (lastDomainCrudVersion?.Version ?? 0) + 1;

        var newDomainCrudVersion = new DomainCrudVersion
        {
            CorrelationId = CorrelationContext.CorrelationId,
            ApplicationId = domainCrud.ApplicationId,
            TenantId = domainCrud.TenantId,
            CrudId = domainCrud.Id,
            TableName = domainCrud.TableName,
            Version = newVersion
        };

        var newDomainCrudVersionColumns =
            domainCrud.DomainCrudColumns.Select(c => new DomainCrudVersionColumn
            {
                CorrelationId = CorrelationContext.CorrelationId,
                CrudVersionId = newDomainCrudVersion.Id,
                PropertyName = c.PropertyName,
                ColumnName = c.ColumnName,
                FieldId = c.FieldId,
                DataTypeId = c.DataTypeId,
                MaxLength = c.MaxLength,
                SortOrder = c.SortOrder
            })
            .ToArray();

        var schema = ResolveSchema(domainCrud.ApplicationId, domainCrud.TenantId);

        string ddl;

        if (_configuration.GetValue<string>("DatabaseType") == "MSSQL")
        {
            var ensureSchemaSql = GenerateEnsureSchemaMsSql(schema);

            var ddlBody = lastDomainCrudVersion is null
                ? GenerateCreateTableMsSql(domainCrud, newDomainCrudVersionColumns)
                : GenerateDiffMsSql(domainCrud, lastDomainCrudVersion.DomainCrudVersionColumns, newDomainCrudVersionColumns);

            ddl = string.IsNullOrWhiteSpace(ddlBody)
                ? ensureSchemaSql
                : ensureSchemaSql + ddlBody;
        }
        else if (_configuration.GetValue<string>("DatabaseType") == "PostgreSQL")
        {
            var ensurePgcryptoSql = GenerateEnsurePgcryptoPgSql();
            var ensureSchemaSql = GenerateEnsureSchemaPgSql(schema);

            var ddlBody = lastDomainCrudVersion is null
                ? GenerateCreateTablePgSql(domainCrud, newDomainCrudVersionColumns)
                : GenerateDiffPgSql(domainCrud, lastDomainCrudVersion.DomainCrudVersionColumns, newDomainCrudVersionColumns);

            ddl = string.IsNullOrWhiteSpace(ddlBody)
                ? ensurePgcryptoSql + ensureSchemaSql
                : ensurePgcryptoSql + ensureSchemaSql + ddlBody;
        }
        else
        {
            return response;
        }

        var crudColsByIdForServices = domainCrud.DomainCrudColumns.ToDictionary(c => c.Id);
        var partialServicesPayload = domainCrud.DomainCrudPartialUpdates
            .Select(pu => new SaveCrudServicesPartialPayloadDto
            {
                PartialCode = pu.Code,
                Columns = pu.Columns
                    .Select(mpc =>
                    {
                        var ccol = crudColsByIdForServices[mpc.CrudColumnId];
                        return new SaveCrudServicesColumnDto { Property = ccol.PropertyName, FieldId = ccol.FieldId };
                    })
                    .ToList()
            })
            .ToList();

        var responseSaveCrudServices = await SaveCrudServicesAsync(
            domainCrud.ApplicationId,
            domainCrud.MicroserviceId,
            domainCrud.Code,
            newDomainCrudVersionColumns,
            partialServicesPayload,
            cancellationToken);

        if (!responseSaveCrudServices.Success)
        {
            response.AddMessage(responseSaveCrudServices.Messages);

            return response;
        }

        if (!string.IsNullOrWhiteSpace(ddl))
        {
            _ = await DbContext.Database.ExecuteSqlRawAsync(ddl, cancellationToken);
        }

        foreach (var col in newDomainCrudVersionColumns)
        {
            col.CrudVersionId = newDomainCrudVersion.Id;
        }

        _ = DbContext.DomainCrudVersions.Add(newDomainCrudVersion);
        DbContext.DomainCrudVersionColumns.AddRange(newDomainCrudVersionColumns);

        var colByProp = newDomainCrudVersionColumns.ToDictionary(c => c.PropertyName, StringComparer.OrdinalIgnoreCase);

        foreach (var mp in domainCrud.DomainCrudPartialUpdates)
        {
            var vp = new DomainCrudVersionPartialUpdate
            {
                CorrelationId = CorrelationContext.CorrelationId,
                TenantId = domainCrud.TenantId,
                CrudVersionId = newDomainCrudVersion.Id,
                Code = mp.Code,
                Name = mp.Name
            };

            _ = DbContext.DomainCrudVersionPartialUpdates.Add(vp);

            foreach (var mpc in mp.Columns)
            {
                var crudCol = crudColsByIdForServices[mpc.CrudColumnId];
                var vcol = colByProp[crudCol.PropertyName];

                DbContext.DomainCrudVersionPartialUpdateColumns.Add(new DomainCrudVersionPartialUpdateColumn
                {
                    CorrelationId = CorrelationContext.CorrelationId,
                    VersionPartialUpdateId = vp.Id,
                    CrudVersionColumnId = vcol.Id
                });
            }
        }

        _ = await DbContext.SaveChangesAsync(cancellationToken);

        return response;
    }

    public virtual async Task<ApiResponse> SaveDomainCrudAsync(
        SaveDomainCrudCommand command,
        CancellationToken cancellationToken)
    {
        var response = new ApiResponse();

        if (command.ApplicationId == Guid.Empty)
        {
            await AddMessage(response, "crud_application_is_required", cancellationToken);

            return response;
        }

        if (command.MicroserviceId == Guid.Empty)
        {
            await AddMessage(response, "crud_microservice_is_required", cancellationToken);

            return response;
        }

        var domainCrud = await DbContext.DomainCruds.FirstOrDefaultAsync(f => f.Id == command.Id, cancellationToken);

        if (domainCrud is null)
        {
            if (!CanCreateDomainCrudDefinition())
            {
                await AddMessage(response, "crud_definition_access_denied", cancellationToken);

                return response;
            }

            domainCrud = new DomainCrud
            {
                Id = command.Id ?? GuidGenerator.New(),
                ApplicationId = command.ApplicationId,
                TenantId = CorrelationContext.TenantId ?? Guid.Empty,
                MicroserviceId = command.MicroserviceId,
                Name = command.NameTr!.ToTranslationString(),
                Code = command.Code,
                TableName = command.TableName,
                Test = command.Test,
                Compensatible = command.Compensatible
            };

            var domainCrudColumns = command.DomainCrudColumns.Select(p => new DomainCrudColumn
            {
                CrudId = domainCrud.Id,
                PropertyName = p.PropertyName,
                ColumnName = p.ColumnName,
                FieldId = p.FieldId,
                DataTypeId = p.DataTypeId,
                MaxLength = p.MaxLength,
                SortOrder = p.SortOrder
            }).ToList();

            _ = DbContext.DomainCruds.Add(domainCrud);

            DbContext.DomainCrudColumns.AddRange(domainCrudColumns);

            var propertyToColumnId = domainCrudColumns.ToDictionary(c => c.PropertyName, c => c.Id, StringComparer.OrdinalIgnoreCase);
            var colById = domainCrudColumns.ToDictionary(c => c.Id);

            SaveNewDomainCrudPartialUpdates(domainCrud, command.DomainCrudPartialUpdates, propertyToColumnId, colById);
        }
        else
        {
            if (!CanMutateDomainCrud(domainCrud))
            {
                await AddMessage(response, "crud_definition_access_denied", cancellationToken);

                return response;
            }

            domainCrud.Name = command.NameTr!.ToTranslationString();
            domainCrud.Code = command.Code;
            domainCrud.TableName = command.TableName;
            domainCrud.Test = command.Test;
            domainCrud.Compensatible = command.Compensatible;

            foreach (var domainCrudColumn in command.DomainCrudColumns ?? [])
            {
                switch (domainCrudColumn._rowStatus)
                {
                    case RowStatuses.New:
                        {
                            var newDomainCrudColumn = new DomainCrudColumn
                            {
                                CrudId = domainCrud.Id,
                                PropertyName = domainCrudColumn.PropertyName,
                                ColumnName = domainCrudColumn.ColumnName,
                                FieldId = domainCrudColumn.FieldId,
                                DataTypeId = domainCrudColumn.DataTypeId,
                                MaxLength = domainCrudColumn.MaxLength,
                                SortOrder = domainCrudColumn.SortOrder
                            };

                            _ = DbContext.DomainCrudColumns.Add(newDomainCrudColumn);

                            break;
                        }

                    case RowStatuses.Edited:
                        {
                            var existingDomainCrudColumn = await DbContext.DomainCrudColumns.FirstOrDefaultAsync(f => f.Id == domainCrudColumn.Id, cancellationToken);

                            if (existingDomainCrudColumn is null)
                            {
                                break;
                            }

                            existingDomainCrudColumn.PropertyName = domainCrudColumn.PropertyName;
                            existingDomainCrudColumn.ColumnName = domainCrudColumn.ColumnName;
                            existingDomainCrudColumn.FieldId = domainCrudColumn.FieldId;
                            existingDomainCrudColumn.DataTypeId = domainCrudColumn.DataTypeId;
                            existingDomainCrudColumn.MaxLength = domainCrudColumn.MaxLength;
                            existingDomainCrudColumn.SortOrder = domainCrudColumn.SortOrder;

                            _ = DbContext.DomainCrudColumns.Update(existingDomainCrudColumn);

                            break;
                        }

                    case RowStatuses.Deleted:
                        {
                            var newDomainCrudColumn = await DbContext.DomainCrudColumns.FirstOrDefaultAsync(f => f.Id == domainCrudColumn.Id, cancellationToken);

                            if (newDomainCrudColumn is null)
                            {
                                break;
                            }

                            _ = DbContext.DomainCrudColumns.Remove(newDomainCrudColumn);

                            break;
                        }
                }
            }

            var persistedCols = await DbContext.DomainCrudColumns
                .Where(c => c.CrudId == domainCrud.Id)
                .ToListAsync(cancellationToken);
            var propToColIdUpdate = persistedCols.ToDictionary(c => c.PropertyName, c => c.Id, StringComparer.OrdinalIgnoreCase);
            var colByIdUpdate = persistedCols.ToDictionary(c => c.Id);

            foreach (var partialCmd in command.DomainCrudPartialUpdates ?? [])
            {
                switch (partialCmd._rowStatus)
                {
                    case RowStatuses.New:
                        {
                            var pu = new DomainCrudPartialUpdate
                            {
                                CorrelationId = CorrelationContext.CorrelationId,
                                TenantId = domainCrud.TenantId,
                                CrudId = domainCrud.Id,
                                Code = partialCmd.Code,
                                Name = partialCmd.Name
                            };
                            _ = DbContext.DomainCrudPartialUpdates.Add(pu);
                            AddPartialUpdateJoinRows(pu.Id, partialCmd.Columns, propToColIdUpdate, colByIdUpdate);
                            break;
                        }
                    case RowStatuses.Edited:
                        {
                            var pu = await DbContext.DomainCrudPartialUpdates
                                .Include(p => p.Columns)
                                .FirstOrDefaultAsync(p => p.Id == partialCmd.Id && p.CrudId == domainCrud.Id, cancellationToken);
                            if (pu is null)
                            {
                                break;
                            }

                            pu.Code = partialCmd.Code;
                            pu.Name = partialCmd.Name;
                            foreach (var link in pu.Columns.ToList())
                            {
                                _ = DbContext.DomainCrudPartialUpdateColumns.Remove(link);
                            }

                            AddPartialUpdateJoinRows(pu.Id, partialCmd.Columns, propToColIdUpdate, colByIdUpdate);
                            break;
                        }
                    case RowStatuses.Deleted:
                        {
                            var pu = await DbContext.DomainCrudPartialUpdates
                                .Include(p => p.Columns)
                                .FirstOrDefaultAsync(p => p.Id == partialCmd.Id && p.CrudId == domainCrud.Id, cancellationToken);
                            if (pu is null)
                            {
                                break;
                            }

                            foreach (var link in pu.Columns.ToList())
                            {
                                _ = DbContext.DomainCrudPartialUpdateColumns.Remove(link);
                            }

                            _ = DbContext.DomainCrudPartialUpdates.Remove(pu);
                            break;
                        }
                }
            }

            _ = DbContext.DomainCruds.Update(domainCrud);
        }

        await SaveTranslations(DbContext.DomainCrudTranslations, domainCrud.Id, nameof(domainCrud.Name), command.NameTr ?? [], cancellationToken);

        _ = await DbContext.SaveChangesAsync(cancellationToken);

        return response;
    }

    public virtual async Task<ApiResponse> DeleteDomainCrudAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = new ApiResponse();

        var domainCrud = await DbContext.DomainCruds
            .Include(s => s.DomainCrudColumns)
            .Include(s => s.DomainCrudPartialUpdates)
                .ThenInclude(p => p.Columns)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (domainCrud is null)
        {
            await AddMessage(response, "crud_definition_not_found", cancellationToken);

            return response;
        }

        if (!CanMutateDomainCrud(domainCrud))
        {
            await AddMessage(response, "crud_definition_access_denied", cancellationToken);

            return response;
        }

        var domainCrudVersions = await DbContext.DomainCrudVersions
            .Include(s => s.DomainCrudVersionColumns)
            .WhereToListAsync(x => x.CrudId == domainCrud.Id, cancellationToken);

        if (domainCrudVersions.Count > 0)
        {
            await AddMessage(response, "crud_definition_can_not_delete_that_published", cancellationToken);

            return response;
        }

        foreach (var pu in domainCrud.DomainCrudPartialUpdates.ToList())
        {
            foreach (var link in pu.Columns.ToList())
            {
                _ = DbContext.DomainCrudPartialUpdateColumns.Remove(link);
            }

            _ = DbContext.DomainCrudPartialUpdates.Remove(pu);
        }

        _ = DbContext.DomainCruds.Remove(domainCrud);

        _ = await DbContext.SaveChangesAsync(cancellationToken);

        return response;
    }

    public virtual async Task<ApiResponse<DomainCrudDto>> GetDomainCrudAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var returnObject = new ApiResponse<DomainCrudDto>();

        var domainCrud = await DbContext.DomainCruds
            .Include(p => p.DomainCrudTranslations)
            .Include(m => m.DomainCrudColumns)
            .Include(m => m.DomainCrudPartialUpdates)
                .ThenInclude(p => p.Columns)
                    .ThenInclude(c => c.CrudColumn)
            .Where(DomainCrudReadFilter())
            .FirstOrDefaultAsync<DomainCrud, DomainCrudDto>(x => x.Id == id, Mapper, cancellationToken);

        returnObject.Value = domainCrud;

        return returnObject;
    }

    public virtual async Task<ApiResponse<DomainCrudDto>> GetDomainCrudByCodeAsync(
        string code,
        CancellationToken cancellationToken)
    {
        var returnObject = new ApiResponse<DomainCrudDto>();

        var domainCrud = await DbContext.DomainCruds
            .Include(p => p.DomainCrudTranslations)
            .Include(m => m.DomainCrudColumns)
            .Include(m => m.DomainCrudPartialUpdates)
                .ThenInclude(p => p.Columns)
                    .ThenInclude(c => c.CrudColumn)
            .Where(DomainCrudReadFilter())
            .FirstOrDefaultAsync<DomainCrud, DomainCrudDto>(x => x.Code == code, Mapper, cancellationToken);

        returnObject.Value = domainCrud;

        return returnObject;
    }

    public virtual async Task<PaginatedApiResponse<DomainCrudsDto>> GetDomainCrudsAsync(
        Guid? applicationId,
        string? name,
        string? code,
        string? q,
        int? pageStart,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        var domainCruds = await DbContext.DomainCruds
            .Include(x => x.DomainCrudTranslations.Where(y => y.LanguageId == CorrelationContext.LanguageId))
            .Where(DomainCrudReadFilter())
            .Where(dc =>
                (!applicationId.HasValue || dc.ApplicationId == applicationId.Value) &&
                (string.IsNullOrEmpty(q) || dc.DomainCrudTranslations.Any(rt => rt.Translation != null && rt.LanguageId == CorrelationContext.LanguageId && rt.Translation.ToLower().Contains(q.ToLower()))) &&
                (string.IsNullOrEmpty(name) || (!string.IsNullOrEmpty(dc.Name) && dc.Name.Contains(name, StringComparison.CurrentCultureIgnoreCase))) &&
                (string.IsNullOrEmpty(code) || (!string.IsNullOrEmpty(dc.Code) && dc.Code.Contains(code, StringComparison.CurrentCultureIgnoreCase))))
            .ToPaginatedListAsync<DomainCrud, DomainCrudsDto>(Mapper, pageStart, pageSize, cancellationToken);

        return domainCruds;
    }

    private Expression<Func<DomainCrud, bool>> DomainCrudReadFilter()
    {
        var ctxTenant = CorrelationContext.TenantId;

        return dc =>
            dc.TenantId == Ids.Customer.System.Id ||
            ctxTenant == null ||
            (ctxTenant.HasValue && dc.TenantId == ctxTenant.Value);
    }

    private static bool IsSystemTenantContext(Guid? ctxTenant) =>
        ctxTenant.HasValue && ctxTenant.Value == Ids.Customer.System.Id;

    private bool CanMutateDomainCrud(DomainCrud dc)
    {
        var ctx = CorrelationContext.TenantId;

        if (IsSystemTenantContext(ctx) || ctx is null)
        {
            return true;
        }

        return ctx.HasValue && dc.TenantId == ctx.Value;
    }

    private bool CanCreateDomainCrudDefinition()
    {
        var ctx = CorrelationContext.TenantId;

        if (IsSystemTenantContext(ctx) || ctx is null)
        {
            return true;
        }

        return ctx.HasValue && ctx.Value != Guid.Empty;
    }

    private void SaveNewDomainCrudPartialUpdates(
        DomainCrud domainCrud,
        IList<SaveDomainCrudCommandPartialUpdate>? partialUpdates,
        Dictionary<string, Guid> propertyToColumnId,
        Dictionary<Guid, DomainCrudColumn> colById)
    {
        foreach (var partialCmd in partialUpdates ?? [])
        {
            if (partialCmd._rowStatus == RowStatuses.Deleted)
            {
                continue;
            }

            var pu = new DomainCrudPartialUpdate
            {
                CorrelationId = CorrelationContext.CorrelationId,
                TenantId = domainCrud.TenantId,
                CrudId = domainCrud.Id,
                Code = partialCmd.Code,
                Name = partialCmd.Name
            };

            _ = DbContext.DomainCrudPartialUpdates.Add(pu);

            AddPartialUpdateJoinRows(pu.Id, partialCmd.Columns, propertyToColumnId, colById);
        }
    }

    private void AddPartialUpdateJoinRows(
        Guid partialUpdateId,
        IList<SaveDomainCrudCommandPartialColumn>? columns,
        Dictionary<string, Guid> propertyToColumnId,
        Dictionary<Guid, DomainCrudColumn> colById)
    {
        foreach (var colCmd in columns ?? [])
        {
            if (!TryResolvePartialCrudColumnId(colCmd, propertyToColumnId, colById, out var crudColumnId))
            {
                continue;
            }

            DbContext.DomainCrudPartialUpdateColumns.Add(new DomainCrudPartialUpdateColumn
            {
                CorrelationId = CorrelationContext.CorrelationId,
                PartialUpdateId = partialUpdateId,
                CrudColumnId = crudColumnId
            });
        }
    }

    private static bool TryResolvePartialCrudColumnId(
        SaveDomainCrudCommandPartialColumn colCmd,
        Dictionary<string, Guid> propertyToColumnId,
        Dictionary<Guid, DomainCrudColumn> colById,
        out Guid crudColumnId)
    {
        if (colCmd.CrudColumnId != Guid.Empty && colById.ContainsKey(colCmd.CrudColumnId))
        {
            crudColumnId = colCmd.CrudColumnId;

            return true;
        }

        if (!string.IsNullOrWhiteSpace(colCmd.ColumnPropertyName) &&
            propertyToColumnId.TryGetValue(colCmd.ColumnPropertyName, out var byProp))
        {
            crudColumnId = byProp;

            return true;
        }

        crudColumnId = Guid.Empty;

        return false;
    }

    private async Task<ApiResponse> SaveCrudServicesAsync(
        Guid applicationId,
        Guid microserviceId,
        string code,
        ICollection<DomainCrudVersionColumn> columns,
        IReadOnlyList<SaveCrudServicesPartialPayloadDto> partials,
        CancellationToken cancellationToken)
    {
        var response = new ApiResponse();

        var columnsParameters = columns?.Select(c => new SaveCrudServicesColumnDto
        {
            Property = c.PropertyName,
            FieldId = c.FieldId
        });

        var partialParameters = partials.Select(p => new Dictionary<string, dynamic>
        {
            { "PartialCode", p.PartialCode },
            { "Columns", p.Columns }
        }).ToList();

        var parameters = new Dictionary<string, dynamic>
            {
                { "ApplicationId", applicationId },
                { "MicroserviceId", microserviceId },
                { "Code", code },
                { "Columns", columnsParameters ?? [] },
                { "Partials", partialParameters }
            };

        var responseApi = await _httpClientsService.CallService<ApiResponse>(
            serviceId: Ids.Service.SaveCrudServices.Id,
            parameters: parameters,
            cancellationToken: cancellationToken);

        if (!responseApi.Success)
        {
            response.AddMessage(responseApi.Messages);

            return response;
        }

        return response;
    }
}

internal class SaveCrudServicesColumnDto
{
    public Guid FieldId { get; set; }
    public required string Property { get; set; }
}

internal sealed class SaveCrudServicesPartialPayloadDto
{
    public required string PartialCode { get; init; }
    public required List<SaveCrudServicesColumnDto> Columns { get; init; }
}