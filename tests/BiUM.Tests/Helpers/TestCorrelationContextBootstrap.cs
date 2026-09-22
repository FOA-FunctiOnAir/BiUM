using BiUM.Core.Authorization;
using BiUM.Infrastructure.Services.Authorization;
using BiUM.Specialized.Database;

namespace BiUM.Tests.Helpers;

public static class TestCorrelationContextBootstrap
{
    private static readonly CorrelationContextAccessor SharedAccessor = new();
    private static int _languageResolverConfigured;

    public static CorrelationContextAccessor SharedAccessorInstance => SharedAccessor;

    public static void EnsureLanguageResolverConfigured()
    {
        if (Interlocked.CompareExchange(ref _languageResolverConfigured, 1, 0) == 0)
        {
            CorrelationContextLanguage.Configure(SharedAccessor);
        }
    }

    public static ICorrelationContextAccessor RegisterSharedAccessor()
    {
        EnsureLanguageResolverConfigured();
        return SharedAccessor;
    }
}