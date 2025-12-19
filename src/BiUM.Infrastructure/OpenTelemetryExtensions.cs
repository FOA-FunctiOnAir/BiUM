using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.DependencyInjection;

public static class OpenTelemetryExtensions
{
    public static OpenTelemetryLoggerOptions AddConsoleExporter(this OpenTelemetryLoggerOptions logging, IHostEnvironment hostEnvironment)
    {
        if (hostEnvironment.IsDevelopment())
        {
            logging.AddConsoleExporter();
        }

        return logging;
    }

    public static MeterProviderBuilder AddConsoleExporter(this MeterProviderBuilder metrics, IConfiguration configuration, IHostEnvironment hostEnvironment)
    {
        if (!hostEnvironment.IsDevelopment())
        {
            return metrics;
        }

        var logMetrics = configuration.GetValue<bool?>("Logging:Console:LogMetrics");

        if (logMetrics != true)
        {
            return  metrics;
        }

        return metrics.AddConsoleExporter();
    }

    public static TracerProviderBuilder AddConsoleExporter(this TracerProviderBuilder tracing, IConfiguration configuration, IHostEnvironment hostEnvironment)
    {
        if (!hostEnvironment.IsDevelopment())
        {
            return tracing;
        }

        var logTracing = configuration.GetValue<bool?>("Logging:Console:LogTracing");

        if (logTracing != true)
        {
            return  tracing;
        }

        return tracing.AddConsoleExporter();
    }
}