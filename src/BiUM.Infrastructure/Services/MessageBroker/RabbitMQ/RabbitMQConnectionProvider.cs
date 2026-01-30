using BiUM.Core.Common.Configs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Infrastructure.Services.MessageBroker.RabbitMQ;

internal sealed class RabbitMQConnectionProvider : IAsyncDisposable
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly BiAppOptions _appOptions;
    private readonly ILogger<RabbitMQConnectionProvider> _logger;

    private readonly SemaphoreSlim _consumerConnectionLock = new(1, 1);
    private readonly SemaphoreSlim _publisherConnectionLock = new(1, 1);

    private IConnection? _consumerConnection;
    private IConnection? _publisherConnection;

    public RabbitMQConnectionProvider(
        IConnectionFactory connectionFactory,
        IOptions<BiAppOptions> appOptionsAccessor,
        ILogger<RabbitMQConnectionProvider> logger)
    {
        _connectionFactory = connectionFactory;
        _appOptions = appOptionsAccessor.Value;
        _logger = logger;
    }

    public async Task<IConnection> GetConsumerConnectionAsync()
    {
        if (_consumerConnection?.IsOpen == true)
        {
            return _consumerConnection;
        }

        await _consumerConnectionLock.WaitAsync();

        try
        {
            if (_consumerConnection?.IsOpen == true)
            {
                return _consumerConnection;
            }

            if (_consumerConnection is not null)
            {
                try
                {
                    await _consumerConnection.CloseAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error closing existing connection");
                }
            }

            var appName = $"{_appOptions.Environment}-{_appOptions.Domain}";

            _consumerConnection = await _connectionFactory.CreateConnectionAsync(appName);

            _consumerConnection.ConnectionShutdownAsync += (_, args) =>
            {
                _logger.LogWarning("RabbitMQ connection shutdown: {Reason}", args.ReplyText);

                return Task.CompletedTask;
            };

            _logger.LogInformation("RabbitMQ connection established");

            return _consumerConnection;
        }
        finally
        {
            _consumerConnectionLock.Release();
        }
    }

    public async Task<IConnection> GetPublisherConnectionAsync()
    {
        if (_publisherConnection?.IsOpen == true)
        {
            return _publisherConnection;
        }

        await _publisherConnectionLock.WaitAsync();

        try
        {
            if (_publisherConnection?.IsOpen == true)
            {
                return _publisherConnection;
            }

            if (_publisherConnection is not null)
            {
                try
                {
                    await _publisherConnection.CloseAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error closing existing connection");
                }
            }

            var appName = $"{_appOptions.Environment}-{_appOptions.Domain}";

            _publisherConnection = await _connectionFactory.CreateConnectionAsync(appName);

            _publisherConnection.ConnectionShutdownAsync += (_, args) =>
            {
                _logger.LogWarning("RabbitMQ connection shutdown: {Reason}", args.ReplyText);

                return Task.CompletedTask;
            };

            _logger.LogInformation("RabbitMQ connection established");

            return _publisherConnection;
        }
        finally
        {
            _publisherConnectionLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        _consumerConnectionLock.Dispose();
        _publisherConnectionLock.Dispose();

        if (_consumerConnection is not null)
        {
            await _consumerConnection.DisposeAsync();
        }

        if (_publisherConnection is not null)
        {
            await _publisherConnection.DisposeAsync();
        }
    }
}