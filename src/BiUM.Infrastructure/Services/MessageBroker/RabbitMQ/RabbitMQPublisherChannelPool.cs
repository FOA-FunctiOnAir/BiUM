using BiUM.Core.Common.Configs;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Infrastructure.Services.MessageBroker.RabbitMQ;

internal sealed class RabbitMQPublisherChannelPool : IAsyncDisposable
{
    private const int DefaultCapacity = 100;

    private readonly RabbitMQConnectionProvider _connectionProvider;

    private readonly int _capacity;

    private readonly ConcurrentQueue<IChannel> _channels = new();

    private int _channelCount;

    public RabbitMQPublisherChannelPool(
        RabbitMQConnectionProvider connectionProvider,
        IOptionsMonitor<RabbitMqOptions> clientOptionsMonitor,
        string clientKey)
    {
        _connectionProvider = connectionProvider;

        var o = clientOptionsMonitor.Get(clientKey);
        _capacity = o.ChannelPoolCapacity <= 0 ? DefaultCapacity : o.ChannelPoolCapacity;
    }

    public async ValueTask<RabbitMQPoolChannel> GetChannelAsync()
    {
        var count = Interlocked.Increment(ref _channelCount);

        if (count > _capacity)
        {
            Interlocked.Decrement(ref _channelCount);
            throw new InvalidOperationException("RabbitMQ channel pool capacity exceeded");
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

        Interlocked.Exchange(ref _channelCount, 0);
    }

    private void ReturnChannel(IChannel channel)
    {
        Interlocked.Decrement(ref _channelCount);

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