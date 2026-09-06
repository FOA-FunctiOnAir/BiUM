using BiUM.Contract.Models.Api;
using BiUM.Core.Constants;
using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Services.DynamicExporter;

public sealed class DynamicExportBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DynamicExportBackgroundService> _logger;

    public DynamicExportBackgroundService(IServiceProvider serviceProvider, ILogger<DynamicExportBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var exporter = scope.ServiceProvider.GetRequiredService<IDynamicExporterService>();
                await exporter.ProcessPendingExportsAsync(stoppingToken);
                await exporter.ExpireOldExportsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dynamic export background loop failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}

public partial class DynamicExporterService
{
    public async Task ProcessPendingExportsAsync(CancellationToken cancellationToken)
    {
        var pending = await DbContext.DomainDynamicExportRequests
            .Where(r => r.Status == Ids.Parameter.DynamicExportRequestStatus.Values.Pending)
            .OrderBy(r => r.Created)
            .Take(5)
            .ToListAsync(cancellationToken);

        foreach (var request in pending)
        {
            await ProcessSingleExportAsync(request, cancellationToken);
        }
    }

    public async Task ExpireOldExportsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var expired = await DbContext.DomainDynamicExportRequests
            .Where(r => r.ExpiresAt != null && r.ExpiresAt < now && r.Status != Ids.Parameter.DynamicExportRequestStatus.Values.Expired)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var request in expired)
        {
            request.Status = Ids.Parameter.DynamicExportRequestStatus.Values.Expired;
            request.ErrorMessage = null;

            var file = await DbContext.DomainDynamicExportFiles
                .FirstOrDefaultAsync(f => f.ExportRequestId == request.Id, cancellationToken);

            if (file is not null)
            {
                _ = DbContext.DomainDynamicExportFiles.Remove(file);
            }
        }

        if (expired.Count > 0)
        {
            await SaveChangesInUnitOfWorkAsync(cancellationToken);
        }
    }

    private async Task ProcessSingleExportAsync(DomainDynamicExportRequest request, CancellationToken cancellationToken)
    {
        request.Status = Ids.Parameter.DynamicExportRequestStatus.Values.Processing;
        await SaveChangesInUnitOfWorkAsync(cancellationToken);

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromMinutes(_options.TotalJobTimeoutMinutes));

            var rows = new List<IDictionary<string, object?>>();
            var pageStart = 0;
            var truncated = false;
            int? sourceTotal = null;

            var sourceParameters = BuildSourceParameters(request.SourceParameters);

            for (var page = 0; page < _options.MaxFetchPages; page++)
            {
                timeoutCts.Token.ThrowIfCancellationRequested();

                var responseJson = await FetchSourcePageAsync(
                    request.SourceUrl,
                    sourceParameters,
                    pageStart,
                    _options.FetchPageSize,
                    timeoutCts.Token);
                var pageRows = DynamicExportExcelWriter.ExtractRowsFromApiResponse(responseJson);

                if (page == 0)
                {
                    sourceTotal = TryReadTotalCount(responseJson);
                }

                if (pageRows.Count == 0)
                {
                    break;
                }

                foreach (var row in pageRows)
                {
                    if (rows.Count >= _options.MaxExportRows)
                    {
                        truncated = true;
                        break;
                    }

                    rows.Add(row);
                }

                if (truncated)
                {
                    break;
                }

                if (sourceTotal is int total && rows.Count >= total)
                {
                    break;
                }

                if (!sourceTotal.HasValue && pageRows.Count < _options.FetchPageSize)
                {
                    break;
                }

                pageStart += pageRows.Count;
            }

            var bytes = DynamicExportExcelWriter.WriteRows(rows);
            var fileName = $"{SanitizeFileName(request.Name)}.{request.Format}";

            var file = await DbContext.DomainDynamicExportFiles
                .FirstOrDefaultAsync(f => f.ExportRequestId == request.Id, cancellationToken);

            if (file is null)
            {
                file = new DomainDynamicExportFile
                {
                    ExportRequestId = request.Id,
                    Content = bytes
                };
                _ = DbContext.DomainDynamicExportFiles.Add(file);
            }
            else
            {
                file.Content = bytes;
                _ = DbContext.DomainDynamicExportFiles.Update(file);
            }

            request.Status = Ids.Parameter.DynamicExportRequestStatus.Values.Ready;
            request.FileName = fileName;
            request.MimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            request.RowCount = rows.Count;
            request.SourceTotalCount = sourceTotal;
            request.Truncated = truncated;
            request.ErrorMessage = null;
        }
        catch (Exception ex)
        {
            request.Status = Ids.Parameter.DynamicExportRequestStatus.Values.Failed;
            request.ErrorMessage = ex.Message;
        }

        await SaveChangesInUnitOfWorkAsync(cancellationToken);
    }

    private Task SaveChangesInUnitOfWorkAsync(CancellationToken cancellationToken) =>
        _unitOfWorkRunner.RunAsync(() => DbContext.SaveChangesAsync(cancellationToken), cancellationToken);

    private async Task<string> FetchSourcePageAsync(
        string sourceUrl,
        Dictionary<string, dynamic> parameters,
        int pageStart,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var response = await _httpClientsService.GetContent(
            sourceUrl,
            parameters,
            external: false,
            pageStart: pageStart,
            pageSize: pageSize,
            cancellationToken: cancellationToken);

        if (!response.Success)
        {
            throw new InvalidOperationException(GetExportFetchErrorMessage(response));
        }

        if (string.IsNullOrWhiteSpace(response.Value))
        {
            throw new InvalidOperationException("export_fetch_failed");
        }

        return response.Value;
    }

    private static string GetExportFetchErrorMessage(ApiResponse response)
    {
        if (response.Messages.Count == 0)
        {
            return "export_fetch_failed";
        }

        return response.Messages[0].Message ?? "export_fetch_failed";
    }

    private static Dictionary<string, dynamic> BuildSourceParameters(string? sourceParametersJson)
    {
        var parameters = new Dictionary<string, dynamic>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(sourceParametersJson))
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(sourceParametersJson);

            if (parsed is not null)
            {
                foreach (var (key, value) in parsed)
                {
                    if (IsPaginationParameterKey(key))
                    {
                        continue;
                    }

                    parameters[key] = value.ValueKind switch
                    {
                        JsonValueKind.String => value.GetString()!,
                        JsonValueKind.Number when value.TryGetInt64(out var l) => l,
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        _ => value.GetRawText()
                    };
                }
            }
        }

        return parameters;
    }

    private static bool IsPaginationParameterKey(string key) =>
        key.Equals("pageStart", StringComparison.OrdinalIgnoreCase)
        || key.Equals("pageSize", StringComparison.OrdinalIgnoreCase);

    private static int? TryReadTotalCount(string responseJson)
    {
        using var doc = JsonDocument.Parse(responseJson);

        if (doc.RootElement.TryGetProperty("totalCount", out var total) && total.TryGetInt32(out var count))
        {
            return count;
        }

        return null;
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var ch in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(ch, '_');
        }

        return string.IsNullOrWhiteSpace(name) ? "export" : name.Trim();
    }
}