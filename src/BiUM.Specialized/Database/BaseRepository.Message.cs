using BiUM.Contract;
using BiUM.Core.Common.API;
using BiUM.Core.Common.Enums;
using BiUM.Core.Database;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Database;

public partial class BaseRepository : IBaseRepository
{
    public virtual async Task<IApiResponse> AddMessage(
        IApiResponse response,
        string code,
        CancellationToken cancellationToken)
    {
        return await _translationService.AddMessage(response, code, string.Empty, MessageSeverity.Error, cancellationToken);
    }

    public virtual async Task<IApiResponse> AddMessage(
        IApiResponse response,
        string code,
        MessageSeverity severity,
        CancellationToken cancellationToken)
    {
        return await _translationService.AddMessage(response, code, string.Empty, severity, cancellationToken);
    }

    public virtual async Task<IApiResponse> AddMessage(
        IApiResponse response,
        string code,
        Exception exception,
        CancellationToken cancellationToken)
    {
        return await _translationService.AddMessage(response, code, exception, MessageSeverity.Error, cancellationToken);
    }

    public virtual async Task<IApiResponse> AddMessage(
        IApiResponse response,
        string code,
        Exception exception,
        MessageSeverity severity,
        CancellationToken cancellationToken)
    {
        return await _translationService.AddMessage(response, code, exception, severity, cancellationToken);
    }

    public virtual async Task<IApiResponse> AddMessage(
        IApiResponse response,
        string code,
        string exception,
        MessageSeverity severity,
        CancellationToken cancellationToken)
    {
        return await _translationService.AddMessage(response, code, exception, severity, cancellationToken);
    }

    public virtual async Task<GrpcResponseMeta> AddMessage(
        GrpcResponseMeta meta,
        string code,
        CancellationToken cancellationToken)
    {
        return await _translationService.AddMessage(meta, code, string.Empty, MessageSeverity.Error, cancellationToken);
    }

    public virtual async Task<GrpcResponseMeta> AddMessage(
        GrpcResponseMeta meta,
        string code,
        MessageSeverity severity,
        CancellationToken cancellationToken)
    {
        return await _translationService.AddMessage(meta, code, string.Empty, severity, cancellationToken);
    }

    public virtual async Task<GrpcResponseMeta> AddMessage(
        GrpcResponseMeta meta,
        string code,
        Exception exception,
        CancellationToken cancellationToken)
    {
        return await _translationService.AddMessage(meta, code, exception.GetFullMessage(), MessageSeverity.Error, cancellationToken);
    }

    public virtual async Task<GrpcResponseMeta> AddMessage(
        GrpcResponseMeta meta,
        string code,
        Exception exception,
        MessageSeverity severity,
        CancellationToken cancellationToken)
    {
        return await _translationService.AddMessage(meta, code, exception, severity, cancellationToken);
    }

    public virtual async Task<GrpcResponseMeta> AddMessage(
        GrpcResponseMeta meta,
        string code,
        string exception,
        MessageSeverity severity,
        CancellationToken cancellationToken)
    {
        return await _translationService.AddMessage(meta, code, exception, severity, cancellationToken);
    }
}
