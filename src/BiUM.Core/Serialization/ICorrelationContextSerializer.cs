using BiUM.Contract.Models;

namespace BiUM.Core.Serialization;

public interface ICorrelationContextSerializer
{
    string Serialize(CorrelationContext context);
    CorrelationContext Deserialize(string correlationContextString);
}
