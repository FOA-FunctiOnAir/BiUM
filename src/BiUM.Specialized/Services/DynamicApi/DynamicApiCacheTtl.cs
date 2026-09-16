using System;

namespace BiUM.Specialized.Services.DynamicApi;

internal static class DynamicApiCacheTtl
{
    internal static readonly TimeSpan Default = TimeSpan.FromDays(1);

    internal static readonly TimeSpan Max = TimeSpan.FromDays(1);

    internal static TimeSpan Normalize(TimeSpan? ttl) =>
        ttl is null || ttl <= TimeSpan.Zero
            ? Default
            : ttl.Value > Max
                ? Max
                : ttl.Value;
}