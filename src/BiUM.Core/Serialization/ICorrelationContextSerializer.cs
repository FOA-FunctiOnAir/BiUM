using BiUM.Contract.Models;
using System;

namespace BiUM.Core.Serialization;

public interface ICorrelationContextSerializer
{
    ReadOnlySpan<byte> Serialize(CorrelationContext value);
    CorrelationContext? Deserialize(ReadOnlySpan<byte> value);
}