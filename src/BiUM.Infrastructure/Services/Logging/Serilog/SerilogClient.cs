using BiUM.Core.Logging.Serilog;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;

namespace BiUM.Infrastructure.Services.Logging.Serilog;

public class SerilogClient : ISerilogClient
{
    private readonly ILogger _logger;

    public SerilogClient(IConfiguration configuration)
    {
        _logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();
    }

    public void Verbose(string messageTemplate, params object[] propertyValues)
    {
        _logger.Verbose(messageTemplate, propertyValues);
    }

    public void Debug(string messageTemplate, params object[] propertyValues)
    {
        _logger.Debug(messageTemplate, propertyValues);
    }

    public void Information(string messageTemplate, params object[] propertyValues)
    {
        _logger.Information(messageTemplate, propertyValues);
    }

    public void Warning(string messageTemplate, params object[] propertyValues)
    {
        _logger.Warning(messageTemplate, propertyValues);
    }

    public void Error(Exception exception, string messageTemplate, params object[] propertyValues)
    {
        _logger.Error(exception, messageTemplate, propertyValues);
    }

    public void Error(string messageTemplate, params object[] propertyValues)
    {
        _logger.Error(messageTemplate, propertyValues);
    }

    public void Fatal(Exception exception, string messageTemplate, params object[] propertyValues)
    {
        _logger.Fatal(exception, messageTemplate, propertyValues);
    }

    public void Fatal(string messageTemplate, params object[] propertyValues)
    {
        _logger.Fatal(messageTemplate, propertyValues);
    }

    public void Dispose()
    {
        //_logger.Dispose();
    }
}