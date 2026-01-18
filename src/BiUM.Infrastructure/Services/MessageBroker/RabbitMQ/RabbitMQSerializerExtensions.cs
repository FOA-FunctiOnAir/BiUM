using BiUM.Core.MessageBroker.RabbitMQ;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Infrastructure.Services.MessageBroker.RabbitMQ;

public static class RabbitMQSerializerExtensions
{
    public static Task<ReadOnlyMemory<byte>> SerializeAsync<T>(
        this IRabbitMQSerializer serializer,
        T value,
        CancellationToken cancellationToken) =>
        serializer.SerializeAsync(value, typeof(T), cancellationToken);

    public static async Task<T?> DeserializeAsync<T>(
        this IRabbitMQSerializer serializer,
        ReadOnlyMemory<byte> value,
        CancellationToken cancellationToken) =>
        (T?) await serializer.DeserializeAsync(value, typeof(T), cancellationToken);
}
