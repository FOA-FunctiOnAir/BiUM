using System;

namespace BiUM.Core.Caching;

public static class CacheKeys
{
    public static class HttpClientService
    {
        public static class Configuration
        {
            private const string Prefix = "bium:httpclient:configuration";

            public static string Service(Guid id) => $"{Prefix}:service:{id}";
            public const string ServicePattern = $"{Prefix}:service:*";
        }
    }
}