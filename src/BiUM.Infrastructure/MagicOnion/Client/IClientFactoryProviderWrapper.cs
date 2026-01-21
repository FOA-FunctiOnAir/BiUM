using MagicOnion;
using MagicOnion.Client;

namespace BiUM.Infrastructure.MagicOnion.Client;

public interface IClientFactoryProviderWrapper<T>
    where T : IService<T>
{
    static abstract IMagicOnionClientFactoryProvider ClientFactoryProvider { get; }
    static abstract IStreamingHubClientFactoryProvider StreamingHubClientFactoryProvider { get; }
    static abstract bool TryRegisterProviderFactory();
    static abstract void RegisterMemoryPackFormatters();
}
