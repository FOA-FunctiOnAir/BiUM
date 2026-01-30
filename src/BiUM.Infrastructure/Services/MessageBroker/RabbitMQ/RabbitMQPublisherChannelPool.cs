using BiUM.Core.Common.Configs;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace BiUM.Infrastructure.Services.MessageBroker.RabbitMQ;

internal sealed class RabbitMQPublisherChannelPool : IAsyncDisposable
{
    private const int DefaultCapacity = 100;

    private readonly RabbitMQConnectionProvider _connectionProvider;
    private readonly RabbitMQOptions _rabbitMqOptions;

    private readonly int _capacity;

    private readonly ConcurrentQueue<IChannel> _channels = new();

    private int _channelCount = 0;
    private object _channelCountLock = new();

    public RabbitMQPublisherChannelPool(
        RabbitMQConnectionProvider connectionProvider,
        IOptions<RabbitMQOptions> rabbitMQOptionsAccessor)
    {
        _rabbitMqOptions = rabbitMQOptionsAccessor.Value;
        _connectionProvider = connectionProvider;
        _capacity = _rabbitMqOptions.ChannelPoolCapacity <= 0 ? DefaultCapacity : _rabbitMqOptions.ChannelPoolCapacity;
    }

    public async ValueTask<RabbitMQPoolChannel> GetChannelAsync()
    {
        lock (_channelCountLock)
        {
            if (_channelCount < _capacity)
            {
                _channelCount++;
            }
            else
            {
                throw new InvalidOperationException("RabbitMQ channel pool capacity exceeded");
            }
        }

        if (_channels.TryDequeue(out var channel))
        {
            if (channel.IsOpen)
            {
                return new RabbitMQPoolChannel(this, channel);
            }

            try
            {
                await channel.CloseAsync();
                await channel.DisposeAsync();
            }
            catch
            {
                // Ignore errors when closing an already closed/bad channel
            }
        }

        var connection = await _connectionProvider.GetPublisherConnectionAsync();

        channel = await connection.CreateChannelAsync();

        return new RabbitMQPoolChannel(this, channel);
    }

    public async ValueTask DisposeAsync()
    {
        while (_channels.TryDequeue(out var channel))
        {
            try
            {
                await channel.CloseAsync();
                await channel.DisposeAsync();
            }
            catch
            {
                // ignored
            }
        }

        lock (_channelCountLock)
        {
            _channelCount = 0;
        }
    }

    private void ReturnChannel(IChannel channel)
    {
        lock (_channelCountLock)
        {
            _channelCount--;
        }

        if (channel.IsOpen)
        {
            _channels.Enqueue(channel);
        }
        else
        {
            try
            {
                channel.Dispose();
            }
            catch
            {
                // ignored
            }
        }
    }

    internal class RabbitMQPoolChannel : IDisposable
    {
        private readonly RabbitMQPublisherChannelPool _pool;

        public IChannel Channel { get; }

        public RabbitMQPoolChannel(RabbitMQPublisherChannelPool pool, IChannel channel)
        {
            _pool = pool;

            Channel = channel;
        }

        public void Dispose()
        {
            _pool.ReturnChannel(Channel);
        }
    }
}