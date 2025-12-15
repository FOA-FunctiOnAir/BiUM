using BiUM.Core.Models;

namespace BiUM.Infrastructure.Services.Serialization;

public interface ICorrelationContextSerializer
{
    string Serialize(CorrelationContext context);
    CorrelationContext Deserialize(string correlationContextString);
}