using BiUM.Core.Models;
using BiUM.Core.Serialization;
using MessagePack;
using MessagePack.Resolvers;
using System;

namespace BiUM.Specialized.Services.Serialization;

public class CorrelationContextSerializer : ICorrelationContextSerializer
{
    private static readonly MessagePackSerializerOptions MessagePackOptions =
        StandardResolver.Options
            .WithOmitAssemblyVersion(true)
            .WithAllowAssemblyVersionMismatch(true)
            .WithCompression(MessagePackCompression.Lz4BlockArray);


    public string Serialize(CorrelationContext value)
    {
        var bytes = MessagePackSerializer.Serialize(value, MessagePackOptions);

        return Convert.ToBase64String(bytes);
    }

    public CorrelationContext Deserialize(string value)
    {
        var bytes = Convert.FromBase64String(value);

        return MessagePackSerializer.Deserialize<CorrelationContext>(bytes, MessagePackOptions);
    }
}