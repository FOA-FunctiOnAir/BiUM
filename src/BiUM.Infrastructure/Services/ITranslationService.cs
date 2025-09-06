using BiUM.Contract;
using BiUM.Core.Common.API;
using BiUM.Core.Common.Enums;

namespace BiUM.Infrastructure.Services;

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
}