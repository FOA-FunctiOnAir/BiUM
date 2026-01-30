using Grpc.Core;
using MagicOnion.Serialization;
using MagicOnion.Serialization.MemoryPack;
using MemoryPack;
using MemoryPack.Compression;
using System.Buffers;
using System.IO.Compression;
using System.Reflection;

namespace BiUM.Infrastructure.MagicOnion.Serialization;

public class MemoryPackWithBrotliSerializerProvider : IMagicOnionSerializerProvider
{
    private readonly MemoryPackSerializerOptions _serializerOptions;
    private readonly CompressionLevel _compressionLevel;

    static MemoryPackWithBrotliSerializerProvider()
    {
        DynamicArgumentTupleFormatter.Register();
    }

    private MemoryPackWithBrotliSerializerProvider(MemoryPackSerializerOptions serializerOptions, CompressionLevel compressionLevel)
    {
        _serializerOptions = serializerOptions;
        _compressionLevel = compressionLevel;
    }

    public static MemoryPackWithBrotliSerializerProvider Create(MemoryPackSerializerOptions serializerOptions, CompressionLevel compressionLevel)
    {
        return new MemoryPackWithBrotliSerializerProvider(serializerOptions, compressionLevel);
    }

    public IMagicOnionSerializer Create(MethodType methodType, MethodInfo? methodInfo)
    {
        return new MagicOnionSerializer(_serializerOptions, _compressionLevel);
    }

    public MemoryPackWithBrotliSerializerProvider WithOptions(MemoryPackSerializerOptions serializerOptions)
        => new(serializerOptions, _compressionLevel);

    public MemoryPackWithBrotliSerializerProvider WithCompressionLevel(CompressionLevel compressionLevel)
        => new(_serializerOptions, compressionLevel);

    private class MagicOnionSerializer : IMagicOnionSerializer
    {
        private readonly MemoryPackSerializerOptions _serializerOptions;
        private readonly CompressionLevel _compressionLevel;

        public MagicOnionSerializer(MemoryPackSerializerOptions serializerOptions, CompressionLevel compressionLevel)
        {
            _serializerOptions = serializerOptions;
            _compressionLevel = compressionLevel;
        }

        public void Serialize<T>(IBufferWriter<byte> writer, in T value)
        {
            using var compressor = new BrotliCompressor(_compressionLevel);

            MemoryPackSerializer.Serialize(compressor, value, _serializerOptions);

            compressor.CopyTo(writer);
        }

        public T Deserialize<T>(in ReadOnlySequence<byte> bytes)
        {
            using var decompressor = new BrotliDecompressor();

            var decompressedBytes = decompressor.Decompress(bytes);

            return MemoryPackSerializer.Deserialize<T>(decompressedBytes, _serializerOptions)!;
        }
    }
}