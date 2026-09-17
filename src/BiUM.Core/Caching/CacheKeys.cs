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

    public static class Translation
    {
        private const string Prefix = "bium:translation";

        public static string Build(string domain, Guid applicationId, Guid languageId, string code) =>
            $"{Prefix}:{domain.ToLowerInvariant()}:{applicationId}:{languageId}:{code}";

        public static string Pattern(string domain, Guid applicationId, string code) =>
            $"{Prefix}:{domain.ToLowerInvariant()}:{applicationId}:*:{code}";
    }
}