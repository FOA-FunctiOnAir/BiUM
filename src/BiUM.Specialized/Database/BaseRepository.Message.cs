using BiUM.Contract.Enums;
using BiUM.Contract.Models.Api;
using BiUM.Core.Database;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Database;

public partial class BaseRepository : IBaseRepository
{
    public virtual Task<ApiResponse> AddMessage(
        ApiResponse response,
        string code,
        CancellationToken cancellationToken)
    {
        return _translationService.AddMessage(response, code, string.Empty, MessageSeverity.Error, cancellationToken);
    }

    public virtual Task<ApiResponse> AddMessage(
        ApiResponse response,
        string code,
        MessageSeverity severity,
        CancellationToken cancellationToken)
    {
        return _translationService.AddMessage(response, code, string.Empty, severity, cancellationToken);
    }

    public virtual Task<ApiResponse> AddMessage(
        ApiResponse response,
        string code,
        Exception exception,
        CancellationToken cancellationToken)
    {
        return _translationService.AddMessage(response, code, exception, MessageSeverity.Error, cancellationToken);
    }

    public virtual Task<ApiResponse> AddMessage(
        ApiResponse response,
        string code,
        Exception exception,
        MessageSeverity severity,
        CancellationToken cancellationToken)
    {
        return _translationService.AddMessage(response, code, exception, severity, cancellationToken);
    }

    public virtual Task<ApiResponse> AddMessage(
        ApiResponse response,
        string code,
        string exception,
        MessageSeverity severity,
        CancellationToken cancellationToken)
    {
        return _translationService.AddMessage(response, code, exception, severity, cancellationToken);
    }
}
