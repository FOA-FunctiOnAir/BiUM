using BiUM.Specialized.Services.DynamicExporter;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Services.BackgroundJobs;

public sealed class DynamicExportBiUMBackgroundJob : IBiUMBackgroundJob
{
    private readonly IServiceProvider _serviceProvider;

    public DynamicExportBiUMBackgroundJob(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var exporter = scope.ServiceProvider.GetRequiredService<IDynamicExporterService>();
        await exporter.ProcessPendingExportsAsync(cancellationToken);
        await exporter.ExpireOldExportsAsync(cancellationToken);
    }
}