using System;

namespace BiUM.Specialized.Services.DynamicApi;

internal static class DynamicApiCacheKeyHelper
{
    internal static string Build(Guid dynamicApiId, string key) =>
        $"{dynamicApiId}-{key}";
}