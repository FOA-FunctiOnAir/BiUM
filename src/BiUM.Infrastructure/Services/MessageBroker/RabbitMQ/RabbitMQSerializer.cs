using BiUM.Core.MessageBroker.RabbitMQ;
using MemoryPack;
using MemoryPack.Compression;
using System;
using System.Buffers;
using System.IO.Compression;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Infrastructure.Services.MessageBroker.RabbitMQ;

internal sealed class RabbitMQSerializer : IRabbitMQSerializer
{
    private readonly MemoryPackSerializerOptions _memoryPackSerializerOptions;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public RabbitMQSerializer(
        JsonSerializerOptions jsonSerializerOptions,
        MemoryPackSerializerOptions memoryPackSerializerOptions)
    {
        _jsonSerializerOptions = jsonSerializerOptions;
        _memoryPackSerializerOptions = memoryPackSerializerOptions;
    }

    public Task<ReadOnlyMemory<byte>> SerializeAsync(object value, Type type, CancellationToken cancellationToken)
    {
        using var compressor = new BrotliCompressor(CompressionLevel.Fastest);

        MemoryPackSerializer.Serialize(type, compressor, value, _memoryPackSerializerOptions);

        var writer = new ArrayBufferWriter<byte>();

        compressor.CopyTo(writer);

        var compressedData = writer.WrittenMemory;

        return Task.FromResult(compressedData);

    }

    public Task<object?> DeserializeAsync(ReadOnlyMemory<byte> value, Type type, CancellationToken cancellationToken)
    {
        using var decompressor = new BrotliDecompressor();

        var decompressedBuffer = decompressor.Decompress(value.Span);

        var deserialized = MemoryPackSerializer.Deserialize(type, decompressedBuffer, _memoryPackSerializerOptions);

        return Task.FromResult(deserialized);
    }
}
