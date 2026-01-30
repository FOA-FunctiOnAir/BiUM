using BiUM.Contract.Enums;
using BiUM.Contract.Models.Api;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Database;

public partial class BaseRepository
{
    protected Task<ApiResponse> AddMessage(
        ApiResponse response,
        string code,
        CancellationToken cancellationToken)
    {
        return TranslationService.AddMessage(response, code, string.Empty, MessageSeverity.Error, cancellationToken);
    }

    protected Task<ApiResponse> AddMessage(
        ApiResponse response,
        string code,
        MessageSeverity severity,
        CancellationToken cancellationToken)
    {
        return TranslationService.AddMessage(response, code, string.Empty, severity, cancellationToken);
    }

    protected Task<ApiResponse> AddMessage(
        ApiResponse response,
        string code,
        Exception exception,
        CancellationToken cancellationToken)
    {
        return TranslationService.AddMessage(response, code, exception, MessageSeverity.Error, cancellationToken);
    }

    protected Task<ApiResponse> AddMessage(
        ApiResponse response,
        string code,
        Exception exception,
        MessageSeverity severity,
        CancellationToken cancellationToken)
    {
        return TranslationService.AddMessage(response, code, exception, severity, cancellationToken);
    }

    protected Task<ApiResponse> AddMessage(
        ApiResponse response,
        string code,
        string exception,
        MessageSeverity severity,
        CancellationToken cancellationToken)
    {
        return TranslationService.AddMessage(response, code, exception, severity, cancellationToken);
    }
}