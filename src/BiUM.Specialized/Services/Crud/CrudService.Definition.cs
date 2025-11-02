using BiUM.Core.Common.Enums;
using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Common.API;
using BiUM.Specialized.Common.Crud;
using BiUM.Specialized.Common.Models;
using BiUM.Specialized.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BiUM.Specialized.Services.Crud;

public partial class CrudService
{
    public virtual async Task<ApiEmptyResponse> PublishDomainCrudAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = new ApiEmptyResponse();

        var domainCrud = await _baseContext.DomainCruds
            .Include(s => s.DomainCrudColumns)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (domainCrud is null)
        {
            return response;
        }

        var lastDomainCrudVersion = await _baseContext.DomainCrudVersions
            .Include(s => s.DomainCrudVersionColumns)
            .OrderByDescending(s => s.Version)
            .FirstOrDefaultAsync(x => x.CrudId == domainCrud.Id, cancellationToken);

        var newVersion = (lastDomainCrudVersion?.Version ?? 0) + 1;

        var newDomainCrudVersion = new DomainCrudVersion()
        {
            CorrelationId = Guid.NewGuid(),
            TenantId = domainCrud.TenantId,
            CrudId = domainCrud.Id,
            TableName = domainCrud.TableName,
            Version = newVersion
        };

        newDomainCrudVersion.DomainCrudVersionColumns = domainCrud.DomainCrudColumns.Select(c => new DomainCrudVersionColumn()
        {
            CorrelationId = Guid.NewGuid(),
            CrudVersionId = newDomainCrudVersion.Id,
            PropertyName = c.PropertyName,
            ColumnName = c.ColumnName,
            DataTypeId = c.DataTypeId,
            MaxLength = c.MaxLength,
            SortOrder = c.SortOrder
        }).ToList();

        var ddl = string.Empty;
        var schema = ResolveSchema(domainCrud.TenantId);

        if (_configuration.GetValue<string>("DatabaseType") == "MSSQL")
        {
            var ensureSchemaSql = GenerateEnsureSchemaMsSql(schema);

            var ddlBody = lastDomainCrudVersion is null
                ? GenerateCreateTableMsSql(domainCrud, newDomainCrudVersion.DomainCrudVersionColumns!)
                : GenerateDiffMsSql(domainCrud, lastDomainCrudVersion.DomainCrudVersionColumns!, newDomainCrudVersion.DomainCrudVersionColumns!);

            ddl = string.IsNullOrWhiteSpace(ddlBody)
                ? ensureSchemaSql
                : (ensureSchemaSql + ddlBody);
        }
        else if (_configuration.GetValue<string>("DatabaseType") == "PostgreSQL")
        {
            var ensurePgcryptoSql = GenerateEnsurePgcryptoPgSql();
            var ensureSchemaSql = GenerateEnsureSchemaPgSql(schema);

            var ddlBody = lastDomainCrudVersion is null
                ? GenerateCreateTablePgSql(domainCrud, newDomainCrudVersion.DomainCrudVersionColumns!)
                : GenerateDiffPgSql(domainCrud, lastDomainCrudVersion.DomainCrudVersionColumns!, newDomainCrudVersion.DomainCrudVersionColumns!);

            ddl = string.IsNullOrWhiteSpace(ddlBody)
                ? (ensurePgcryptoSql + ensureSchemaSql)
                : (ensurePgcryptoSql + ensureSchemaSql + ddlBody);
        }
        else
        {
            return response;
        }

        //using var tx = await _baseContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            if (!string.IsNullOrWhiteSpace(ddl))
            {
                await _baseContext.Database.ExecuteSqlRawAsync(ddl, cancellationToken);
            }

            foreach (var col in newDomainCrudVersion.DomainCrudVersionColumns!)
            {
                col.CrudVersionId = newDomainCrudVersion.Id;
            }

            _baseContext.DomainCrudVersions.Add(newDomainCrudVersion);

            await _baseContext.SaveChangesAsync(cancellationToken);

            //await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            //await tx.RollbackAsync(cancellationToken);
        }

        return response;
    }

    public virtual async Task<ApiEmptyResponse> SaveDomainCrudAsync(
        SaveDomainCrudCommand command,
        CancellationToken cancellationToken)
    {
        var response = new ApiEmptyResponse();

        var domainCrud = await _baseContext.DomainCruds.FirstOrDefaultAsync(f => f.Id == command.Id, cancellationToken);

        if (domainCrud is null)
        {
            domainCrud = new DomainCrud()
            {
                TenantId = _currentUserService.TenantId!.Value,
                MicroserviceId = command.MicroserviceId,
                Name = command.NameTr.ToTranslationString(),
                Code = command.Code,
                TableName = command.TableName,
                Test = command.Test
            };

            domainCrud.DomainCrudColumns = command.DomainCrudColumns?.Select(p => new DomainCrudColumn
            {
                CrudId = domainCrud.Id,
                PropertyName = p.PropertyName,
                ColumnName = p.ColumnName,
                DataTypeId = p.DataTypeId,
                MaxLength = p.MaxLength,
                SortOrder = p.SortOrder
            }).ToList();

            _baseContext.DomainCruds.Add(domainCrud);
        }
        else
        {
            domainCrud.Name = command.NameTr.ToTranslationString();
            domainCrud.Code = command.Code;
            domainCrud.TableName = command.TableName;
            domainCrud.Test = command.Test;

            foreach (var domainCrudColumn in command.DomainCrudColumns)
            {
                if (domainCrudColumn._rowStatus == RowStatuses.New)
                {
                    var newDomainCrudColumn = new DomainCrudColumn
                    {
                        CrudId = domainCrud.Id,
                        PropertyName = domainCrudColumn.PropertyName,
                        ColumnName = domainCrudColumn.ColumnName,
                        DataTypeId = domainCrudColumn.DataTypeId,
                        MaxLength = domainCrudColumn.MaxLength,
                        SortOrder = domainCrudColumn.SortOrder
                    };

                    _baseContext.DomainCrudColumns.Add(newDomainCrudColumn);
                }
                else if (domainCrudColumn._rowStatus == RowStatuses.Edited)
                {
                    var newDomainCrudColumn = await _baseContext.DomainCrudColumns.FirstOrDefaultAsync(f => f.Id == domainCrudColumn.Id, cancellationToken);
                    newDomainCrudColumn!.PropertyName = domainCrudColumn.PropertyName;
                    newDomainCrudColumn!.ColumnName = domainCrudColumn.ColumnName;
                    newDomainCrudColumn!.DataTypeId = domainCrudColumn.DataTypeId;
                    newDomainCrudColumn!.MaxLength = domainCrudColumn.MaxLength;
                    newDomainCrudColumn!.SortOrder = domainCrudColumn.SortOrder;

                    _baseContext.DomainCrudColumns.Update(newDomainCrudColumn);
                }
                else if (domainCrudColumn._rowStatus == RowStatuses.Deleted)
                {
                    var newDomainCrudColumn = await _baseContext.DomainCrudColumns.FirstOrDefaultAsync(f => f.Id == domainCrudColumn.Id, cancellationToken);

                    _baseContext.DomainCrudColumns.Remove(newDomainCrudColumn!);
                }
            }

            _baseContext.DomainCruds.Update(domainCrud);
        }

        await SaveTranslations(_baseContext.DomainCrudTranslations, domainCrud!.Id, nameof(domainCrud.Name), command.NameTr, cancellationToken);

        await _baseContext.SaveChangesAsync(cancellationToken);

        return response;
    }

    public virtual async Task<ApiEmptyResponse> DeleteDomainCrudAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = new ApiEmptyResponse();

        var domainCrud = await _baseContext.DomainCruds
            .Include(s => s.DomainCrudColumns)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (domainCrud is null)
        {
            response.AddMessage("Domain Crud not found", MessageSeverity.Error);

            return response;
        }

        var domainCrudVersions = await _baseContext.DomainCrudVersions
            .Include(s => s.DomainCrudVersionColumns)
            .WhereToListAsync(x => x.CrudId == domainCrud.Id, cancellationToken);

        if (domainCrudVersions is not null && domainCrudVersions.Count > 0)
        {
            response.AddMessage("You can not delete published Domain Crud", MessageSeverity.Error);

            return response;
        }

        _baseContext.DomainCruds.Remove(domainCrud);

        await _baseContext.SaveChangesAsync(cancellationToken);

        return response;
    }

    public virtual async Task<ApiResponse<DomainCrudDto>> GetDomainCrudAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var returnObject = new ApiResponse<DomainCrudDto>();

        var domainCrud = await _baseContext.DomainCruds
            .Include(p => p.DomainCrudTranslations)
            .Include(m => m.DomainCrudColumns)
            .FirstOrDefaultAsync<DomainCrud, DomainCrudDto>(x => x.Id == id, _mapper, cancellationToken);

        returnObject.Value = domainCrud;

        return returnObject;
    }

    public virtual async Task<ApiResponse<DomainCrudDto>> GetDomainCrudByCodeAsync(
        string code,
        CancellationToken cancellationToken)
    {
        var returnObject = new ApiResponse<DomainCrudDto>();

        var domainCrud = await _baseContext.DomainCruds
            .Include(p => p.DomainCrudTranslations)
            .Include(m => m.DomainCrudColumns)
            .FirstOrDefaultAsync<DomainCrud, DomainCrudDto>(x => x.Code == code, _mapper, cancellationToken);

        returnObject.Value = domainCrud;

        return returnObject;
    }

    public virtual async Task<PaginatedApiResponse<DomainCrudsDto>> GetDomainCrudsAsync(
        string? name,
        string? code,
        string? q,
        int? pageStart,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        var domainCruds = await _baseContext.DomainCruds
            .Include(x => x.DomainCrudTranslations!.Where(y => y.LanguageId == _currentUserService.LanguageId))
            .Where(a =>
                (string.IsNullOrEmpty(q) || a.DomainCrudTranslations!.Any(rt => rt.Translation != null && rt.LanguageId == _currentUserService.LanguageId && rt.Translation.ToLower().Contains(q.ToLower()))) &&
                (string.IsNullOrEmpty(name) || (!string.IsNullOrEmpty(a.Name) && a.Name.ToLower().Contains(name.ToLower()))) &&
                (string.IsNullOrEmpty(code) || (!string.IsNullOrEmpty(a.Code) && a.Code.ToLower().Contains(code.ToLower()))))
            .ToPaginatedListAsync<DomainCrud, DomainCrudsDto>(_mapper, pageStart, pageSize, cancellationToken);

        return domainCruds;
    }
}