using BiUM.Contract.Enums;
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
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (domainCrud is null)
        {
            response.AddMessage("Crud definition not found", MessageSeverity.Error);

            return response;
        }

        if (domainCrud.ApplicationId == Guid.Empty)
        {
            response.AddMessage("Crud Application is required", MessageSeverity.Error);

            return response;
        }

        if (domainCrud.MicroserviceId == Guid.Empty)
        {
            response.AddMessage("Crud Microservice is required", MessageSeverity.Error);

            return response;
        }

        if (string.IsNullOrEmpty(domainCrud.Code))
        {
            response.AddMessage("Crud Code is required", MessageSeverity.Error);

            return response;
        }

        if (domainCrud.Code.Length < 3)
        {
            response.AddMessage("Crud Code should be min 3 char", MessageSeverity.Error);

            return response;
        }

        if (string.IsNullOrEmpty(domainCrud.TableName))
        {
            response.AddMessage("Crud Table Name is required", MessageSeverity.Error);

            return response;
        }

        if (domainCrud.TableName.Length < 3)
        {
            response.AddMessage("Crud Table Name should be min 3 char", MessageSeverity.Error);

            return response;
        }

        if (domainCrud.DomainCrudColumns.Count == 0)
        {
            response.AddMessage("Crud should have columns", MessageSeverity.Error);

            return response;
        }

        if (domainCrud.DomainCrudColumns.Any(cc => cc.FieldId == Guid.Empty))
        {
            response.AddMessage("Crud Columns should be field", MessageSeverity.Error);

            return response;
        }

        if (domainCrud.DomainCrudColumns.Any(cc => cc.DataTypeId == Guid.Empty))
        {
            response.AddMessage("Crud Columns should be data type", MessageSeverity.Error);

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

        // using var transaction = await DbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var responseSaveCrudServices = await SaveCrudServicesAsync(domainCrud.MicroserviceId, domainCrud.Code, newDomainCrudVersionColumns, cancellationToken);

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

            _ = await DbContext.SaveChangesAsync(cancellationToken);

            // await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            // await transaction.RollbackAsync(cancellationToken);
        }

        return response;
    }

    public virtual async Task<ApiResponse> SaveDomainCrudAsync(
        SaveDomainCrudCommand command,
        CancellationToken cancellationToken)
    {
        var response = new ApiResponse();

        if (command.ApplicationId == Guid.Empty)
        {
            response.AddMessage("Application is required", MessageSeverity.Error);

            return response;
        }

        if (command.MicroserviceId == Guid.Empty)
        {
            response.AddMessage("Microservice is required", MessageSeverity.Error);

            return response;
        }

        var domainCrud = await DbContext.DomainCruds.FirstOrDefaultAsync(f => f.Id == command.Id, cancellationToken);

        if (domainCrud is null)
        {
            domainCrud = new DomainCrud
            {
                Id = command.Id ?? GuidGenerator.New(),
                ApplicationId = command.ApplicationId,
                TenantId = CorrelationContext.TenantId!.Value,
                MicroserviceId = command.MicroserviceId,
                Name = command.NameTr!.ToTranslationString(),
                Code = command.Code,
                TableName = command.TableName,
                Test = command.Test
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
            });

            _ = DbContext.DomainCruds.Add(domainCrud);
            DbContext.DomainCrudColumns.AddRange(domainCrudColumns);
        }
        else
        {
            domainCrud.Name = command.NameTr!.ToTranslationString();
            domainCrud.Code = command.Code;
            domainCrud.TableName = command.TableName;
            domainCrud.Test = command.Test;

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
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (domainCrud is null)
        {
            response.AddMessage("Domain Crud not found", MessageSeverity.Error);

            return response;
        }

        var domainCrudVersions = await DbContext.DomainCrudVersions
            .Include(s => s.DomainCrudVersionColumns)
            .WhereToListAsync(x => x.CrudId == domainCrud.Id, cancellationToken);

        if (domainCrudVersions.Count > 0)
        {
            response.AddMessage("You can not delete published Domain Crud", MessageSeverity.Error);

            return response;
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
            .Where(dc =>
                (!applicationId.HasValue || dc.ApplicationId == applicationId.Value) &&
                (string.IsNullOrEmpty(q) || dc.DomainCrudTranslations.Any(rt => rt.Translation != null && rt.LanguageId == CorrelationContext.LanguageId && rt.Translation.ToLower().Contains(q.ToLower()))) &&
                (string.IsNullOrEmpty(name) || (!string.IsNullOrEmpty(dc.Name) && dc.Name.Contains(name, StringComparison.CurrentCultureIgnoreCase))) &&
                (string.IsNullOrEmpty(code) || (!string.IsNullOrEmpty(dc.Code) && dc.Code.Contains(code, StringComparison.CurrentCultureIgnoreCase))))
            .ToPaginatedListAsync<DomainCrud, DomainCrudsDto>(Mapper, pageStart, pageSize, cancellationToken);

        return domainCruds;
    }

    private async Task<ApiResponse> SaveCrudServicesAsync(Guid microserviceId, string code, ICollection<DomainCrudVersionColumn> columns, CancellationToken cancellationToken)
    {
        var response = new ApiResponse();

        var columnsParameters = columns?.Select(c => new SaveCrudServicesColumnDto
        {
            Property = c.PropertyName,
            FieldId = c.FieldId
        });

        var parameters = new Dictionary<string, dynamic>
            {
                { "MicroserviceId", microserviceId },
                { "Code", code },
                { "Columns", columnsParameters ?? Array.Empty<SaveCrudServicesColumnDto>() }
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