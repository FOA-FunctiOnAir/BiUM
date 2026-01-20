using BiUM.Contract.Models;
using BiUM.Core.Serialization;
using MemoryPack;
using MemoryPack.Compression;
using System;
using System.IO.Compression;

namespace BiUM.Specialized.Services.Serialization;

public sealed class CorrelationContextSerializer : ICorrelationContextSerializer
{
    private readonly MemoryPackSerializerOptions _memoryPackSerializerOptions;

    public CorrelationContextSerializer(MemoryPackSerializerOptions memoryPackSerializerOptions)
    {
        _memoryPackSerializerOptions = memoryPackSerializerOptions;
    }

    public ReadOnlySpan<byte> Serialize(CorrelationContext value)
    {
        using var compressor = new BrotliCompressor(CompressionLevel.Fastest);

        MemoryPackSerializer.Serialize(compressor, value, _memoryPackSerializerOptions);

        var compressedBytes = compressor.ToArray();

        return compressedBytes;
    }

    public CorrelationContext? Deserialize(ReadOnlySpan<byte> value)
    {
        using var decompressor = new BrotliDecompressor();

        var decompressedBuffer = decompressor.Decompress(value);

        var correlationContext = MemoryPackSerializer.Deserialize<CorrelationContext>(decompressedBuffer, _memoryPackSerializerOptions);

        return correlationContext;
    }
}
