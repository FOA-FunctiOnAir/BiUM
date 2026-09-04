using BiUM.Contract.Models.Api;
using BiUM.Core.Common.Utils;
using BiUM.Core.Constants;
using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Common.DynamicApi;
using BiUM.Specialized.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Services.DynamicExporter;

public partial class DynamicExporterService
{
    public async Task<ApiResponse<DynamicExportRequestDto>> SaveExportRequestAsync(
        SaveExportRequestCommand command,
        CancellationToken cancellationToken)
    {
        var response = new ApiResponse<DynamicExportRequestDto>();

        var userId = CorrelationContext.User?.Id;

        if (userId is null || userId == Guid.Empty)
        {
            await AddMessage(response, "export_user_required", cancellationToken);

            return response;
        }

        var request = new DomainDynamicExportRequest
        {
            Id = GuidGenerator.New(),
            ApplicationId = command.ApplicationId ?? (CorrelationContext.ApplicationId != Guid.Empty ? CorrelationContext.ApplicationId : Guid.Empty),
            Name = command.Name,
            Status = Ids.Parameter.DynamicExportRequestStatus.Values.Pending,
            SourceUrl = command.SourceUrl,
            SourceMicroserviceId = command.SourceMicroserviceId,
            SourceParameters = command.SourceParameters is null ? null : JsonSerializer.Serialize(command.SourceParameters),
            Format = string.IsNullOrWhiteSpace(command.Format) ? "xlsx" : command.Format.Trim().ToLowerInvariant(),
            ExpiresAt = DateTime.UtcNow.AddDays(_options.TtlDays)
        };

        _ = DbContext.DomainDynamicExportRequests.Add(request);
        _ = await DbContext.SaveChangesAsync(cancellationToken);

        response.Value = MapRequest(request);

        return response;
    }

    public async Task<ApiResponse<DynamicExportRequestDto>> GetExportRequestAsync(Guid id, CancellationToken cancellationToken)
    {
        var response = new ApiResponse<DynamicExportRequestDto>();
        var request = await FindOwnedRequestAsync(id, cancellationToken);

        if (request is null)
        {
            await AddMessage(response, "export_request_not_found", cancellationToken);

            return response;
        }

        response.Value = MapRequest(request);

        return response;
    }

    public async Task<PaginatedApiResponse<DynamicExportRequestDto>> GetExportRequestsAsync(
        GetExportRequestsQuery query,
        CancellationToken cancellationToken)
    {
        if (!TryGetOwnedExportScope(out _))
        {
            return new PaginatedApiResponse<DynamicExportRequestDto>();
        }

        return await OwnedExportRequests()
            .OrderByDescending(r => r.Created)
            .ToPaginatedListAsync<DomainDynamicExportRequest, DynamicExportRequestDto>(
                PaginationQuery.ToPageBaseQuery(query.PageStart, query.PageSize),
                Mapper,
                cancellationToken);
    }

    public async Task<ApiResponse> DeleteExportRequestAsync(Guid id, CancellationToken cancellationToken)
    {
        var response = new ApiResponse();
        var request = await FindOwnedRequestAsync(id, cancellationToken);

        if (request is null)
        {
            await AddMessage(response, "export_request_not_found", cancellationToken);

            return response;
        }

        var file = await DbContext.DomainDynamicExportFiles
            .FirstOrDefaultAsync(f => f.ExportRequestId == id, cancellationToken);

        if (file is not null)
        {
            _ = DbContext.DomainDynamicExportFiles.Remove(file);
        }

        _ = DbContext.DomainDynamicExportRequests.Remove(request);
        _ = await DbContext.SaveChangesAsync(cancellationToken);

        return response;
    }

    public async Task<(byte[] Content, string FileName, string MimeType)?> DownloadAsync(Guid id, CancellationToken cancellationToken)
    {
        var request = await FindOwnedRequestAsync(id, cancellationToken);

        if (request is null || request.Status != Ids.Parameter.DynamicExportRequestStatus.Values.Ready)
        {
            return null;
        }

        var file = await DbContext.DomainDynamicExportFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.ExportRequestId == id, cancellationToken);

        if (file?.Content is null || file.Content.Length == 0)
        {
            return null;
        }

        return (file.Content, request.FileName ?? $"{request.Name}.xlsx", request.MimeType ?? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    private async Task<DomainDynamicExportRequest?> FindOwnedRequestAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetOwnedExportScope(out _))
        {
            return null;
        }

        return await OwnedExportRequests()
           .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    private IQueryable<DomainDynamicExportRequest> OwnedExportRequests()
    {
        var scope = BuildOwnedExportScopeFilter();

        return DbContext.DomainDynamicExportRequests.Where(scope);
    }

    private bool TryGetOwnedExportScope(out Guid userId)
    {
        userId = CorrelationContext.User?.Id ?? Guid.Empty;

        return userId != Guid.Empty;
    }

    private Expression<Func<DomainDynamicExportRequest, bool>> BuildOwnedExportScopeFilter()
    {
        var userId = CorrelationContext.User?.Id ?? Guid.Empty;
        var tenantId = CorrelationContext.TenantId ?? Guid.Empty;
        var applicationId = CorrelationContext.ApplicationId;

        return r =>
            r.CreatedBy == userId &&
            r.TenantId == tenantId &&
            r.ApplicationId == applicationId;
    }

    private static DynamicExportRequestDto MapRequest(DomainDynamicExportRequest request)
    {
        return new DynamicExportRequestDto
        {
            Id = request.Id,
            ApplicationId = request.ApplicationId,
            Name = request.Name,
            Status = request.Status,
            SourceUrl = request.SourceUrl,
            SourceMicroserviceId = request.SourceMicroserviceId,
            SourceParameters = request.SourceParameters,
            Format = request.Format,
            FileName = request.FileName,
            MimeType = request.MimeType,
            RowCount = request.RowCount,
            SourceTotalCount = request.SourceTotalCount,
            Truncated = request.Truncated,
            ErrorMessage = request.ErrorMessage,
            ExpiresAt = request.ExpiresAt
        };
    }
}