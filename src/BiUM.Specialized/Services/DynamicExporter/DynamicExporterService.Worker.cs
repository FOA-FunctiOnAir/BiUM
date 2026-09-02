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
            _ = await DbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task ProcessSingleExportAsync(DomainDynamicExportRequest request, CancellationToken cancellationToken)
    {
        request.Status = Ids.Parameter.DynamicExportRequestStatus.Values.Processing;
        _ = await DbContext.SaveChangesAsync(cancellationToken);

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromMinutes(_options.TotalJobTimeoutMinutes));

            var rows = new List<IDictionary<string, object?>>();
            var pageStart = 0;
            var truncated = false;
            int? sourceTotal = null;

            for (var page = 0; page < _options.MaxFetchPages; page++)
            {
                timeoutCts.Token.ThrowIfCancellationRequested();

                var pageParameters = BuildPageParameters(request.SourceParameters, pageStart, _options.FetchPageSize);
                var responseJson = await FetchSourcePageAsync(request.SourceUrl, pageParameters, timeoutCts.Token);
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

                if (truncated || pageRows.Count < _options.FetchPageSize)
                {
                    break;
                }

                pageStart += _options.FetchPageSize;
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

        _ = await DbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> FetchSourcePageAsync(
        string sourceUrl,
        Dictionary<string, dynamic> parameters,
        CancellationToken cancellationToken)
    {
        using var perPageCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        perPageCts.CancelAfter(TimeSpan.FromSeconds(_options.PerPageTimeoutSeconds));

        var pageStart = parameters.TryGetValue("pageStart", out var ps) ? Convert.ToInt32(ps) : (int?)null;
        var pageSize = parameters.TryGetValue("pageSize", out var psz) ? Convert.ToInt32(psz) : (int?)null;

        var response = await _httpClientsService.Get<ApiResponse>(
            sourceUrl,
            parameters,
            external: false,
            pageStart: pageStart,
            pageSize: pageSize,
            cancellationToken: perPageCts.Token);

        if (!response.Success)
        {
            var message = response.Messages.FirstOrDefault()?.Message ?? "export_fetch_failed";
            throw new InvalidOperationException(message);
        }

        return JsonSerializer.Serialize(response);
    }

    private static Dictionary<string, dynamic> BuildPageParameters(string? sourceParametersJson, int pageStart, int pageSize)
    {
        var parameters = new Dictionary<string, dynamic>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(sourceParametersJson))
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(sourceParametersJson);

            if (parsed is not null)
            {
                foreach (var (key, value) in parsed)
                {
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

        parameters["pageStart"] = pageStart;
        parameters["pageSize"] = pageSize;

        return parameters;
    }

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