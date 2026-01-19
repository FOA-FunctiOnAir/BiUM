using BiUM.Contract.Enums;
using BiUM.Contract.Models.Api;
using BiUM.Specialized.Common.Translation;
using System;
using System.Threading;
using System.Threading.Tasks;
using ResponseMeta = BiUM.Contract.Models.Api.ResponseMeta;

namespace BiUM.Specialized.Services;

public interface ITranslationService
{
    Task<ApiResponse> AddMessage(
        ApiResponse response,
        string code,
        CancellationToken cancellationToken);

    Task<ApiResponse> AddMessage(
        ApiResponse response,
        string code,
        MessageSeverity severity,
        CancellationToken cancellationToken);

    Task<ApiResponse> AddMessage(
        ApiResponse response,
        string code,
        Exception exception,
        CancellationToken cancellationToken);

    Task<ApiResponse> AddMessage(
        ApiResponse response,
        string code,
        Exception exception,
        MessageSeverity severity,
        CancellationToken cancellationToken);

    Task<ApiResponse> AddMessage(
        ApiResponse response,
        string code,
        string exception,
        MessageSeverity severity,
        CancellationToken cancellationToken);

    Task<ResponseMeta> AddMessage(
        ResponseMeta meta,
        string code,
        CancellationToken cancellationToken);

    Task<ResponseMeta> AddMessage(
        ResponseMeta meta,
        string code,
        MessageSeverity severity,
        CancellationToken cancellationToken);

    Task<ResponseMeta> AddMessage(
        ResponseMeta meta,
        string code,
        Exception exception,
        CancellationToken cancellationToken);

    Task<ResponseMeta> AddMessage(
        ResponseMeta meta,
        string code,
        Exception exception,
        MessageSeverity severity,
        CancellationToken cancellationToken);

    Task<ResponseMeta> AddMessage(
        ResponseMeta meta,
        string code,
        string exception,
        MessageSeverity severity,
        CancellationToken cancellationToken);

    Task<ApiResponse> SaveDomainTranslationAsync(
        SaveDomainTranslationCommand command,
        CancellationToken cancellationToken);

    Task<ApiResponse> DeleteDomainTranslationAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<ApiResponse<DomainTranslationDto>> GetDomainTranslationAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<PaginatedApiResponse<DomainTranslationsDto>> GetDomainTranslationsAsync(
        string? code,
        string? q,
        int? pageStart,
        int? pageSize,
        CancellationToken cancellationToken);
}
