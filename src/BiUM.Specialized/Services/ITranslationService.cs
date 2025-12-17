using BiUM.Contract;
using BiUM.Core.Common.API;
using BiUM.Core.Common.Enums;
using BiUM.Specialized.Common.API;
using BiUM.Specialized.Common.Translation;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Services;

public interface ITranslationService
{
    Task<IApiResponse> AddMessage(
        IApiResponse response,
        string code,
        CancellationToken cancellationToken);

    Task<IApiResponse> AddMessage(
        IApiResponse response,
        string code,
        MessageSeverity severity,
        CancellationToken cancellationToken);

    Task<IApiResponse> AddMessage(
        IApiResponse response,
        string code,
        Exception exception,
        CancellationToken cancellationToken);

    Task<IApiResponse> AddMessage(
        IApiResponse response,
        string code,
        Exception exception,
        MessageSeverity severity,
        CancellationToken cancellationToken);

    Task<IApiResponse> AddMessage(
        IApiResponse response,
        string code,
        string exception,
        MessageSeverity severity,
        CancellationToken cancellationToken);

    Task<GrpcResponseMeta> AddMessage(
        GrpcResponseMeta meta,
        string code,
        CancellationToken cancellationToken);

    Task<GrpcResponseMeta> AddMessage(
        GrpcResponseMeta meta,
        string code,
        MessageSeverity severity,
        CancellationToken cancellationToken);

    Task<GrpcResponseMeta> AddMessage(
        GrpcResponseMeta meta,
        string code,
        Exception exception,
        CancellationToken cancellationToken);

    Task<GrpcResponseMeta> AddMessage(
        GrpcResponseMeta meta,
        string code,
        Exception exception,
        MessageSeverity severity,
        CancellationToken cancellationToken);

    Task<GrpcResponseMeta> AddMessage(
        GrpcResponseMeta meta,
        string code,
        string exception,
        MessageSeverity severity,
        CancellationToken cancellationToken);

    Task<ApiEmptyResponse> SaveDomainTranslationAsync(
        SaveDomainTranslationCommand command,
        CancellationToken cancellationToken);

    Task<ApiEmptyResponse> DeleteDomainTranslationAsync(
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