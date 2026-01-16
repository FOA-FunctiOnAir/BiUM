using BiUM.Contract.Models;
using BiUM.Core.Serialization;
using MemoryPack;
using MemoryPack.Compression;
using System;
using System.IO.Compression;

namespace BiUM.Specialized.Services.Serialization;

public class CorrelationContextSerializer : ICorrelationContextSerializer
{
    private static readonly MemoryPackSerializerOptions MemoryPackSerializerOptions =
        MemoryPackSerializerOptions.Default;


    public string Serialize(CorrelationContext value)
    {
        using var compressor = new BrotliCompressor(CompressionLevel.Fastest);

        MemoryPackSerializer.Serialize(compressor, value, MemoryPackSerializerOptions);

        var compressedBytes = compressor.ToArray();

        var base64String = Convert.ToBase64String(compressedBytes);

        return base64String;
    }

    public CorrelationContext Deserialize(string value)
    {
        var compressedBytes = Convert.FromBase64String(value);

        using var decompressor = new BrotliDecompressor();

        var decompressedBuffer = decompressor.Decompress(compressedBytes);

        var correlationContext = MemoryPackSerializer.Deserialize<CorrelationContext>(decompressedBuffer, MemoryPackSerializerOptions);

        return correlationContext ?? CorrelationContext.Empty;
    }
}
