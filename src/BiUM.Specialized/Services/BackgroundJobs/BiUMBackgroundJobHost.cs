using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Services.BackgroundJobs;

public sealed class BiUMBackgroundJobHost : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEnumerable<BiUMBackgroundJobRegistration> _jobs;
    private readonly ILogger<BiUMBackgroundJobHost> _logger;

    public BiUMBackgroundJobHost(
        IServiceProvider serviceProvider,
        IEnumerable<BiUMBackgroundJobRegistration> jobs,
        ILogger<BiUMBackgroundJobHost> logger)
    {
        _serviceProvider = serviceProvider;
        _jobs = jobs;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var job in _jobs)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var instance = (IBiUMBackgroundJob)scope.ServiceProvider.GetRequiredService(job.JobType);
                    await instance.ExecuteAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "BiUM background job {JobType} failed", job.JobType.Name);
                }

                await Task.Delay(job.Interval, stoppingToken);
            }
        }
    }
}

public sealed class BiUMBackgroundJobRegistration
{
    public required Type JobType { get; init; }

    public TimeSpan Interval { get; init; } = TimeSpan.FromSeconds(10);
}