namespace BiUM.Core.Constants;

public static class HeaderKeys
{
    public static readonly string ApplicationId = "x-application-id";
    public static readonly string CorrelationContext = "x-correlation-context";
    public static readonly string CorrelationId = "x-correlation-id";
    public static readonly string TraceId = "x-trace-id";
    public static readonly string LanguageId = "x-language-id";
    public static readonly string TenantId = "x-tenant-id";

    public static readonly string ClientIp = "x-client-ip";
    public static readonly string ClientHost = "x-client-host";

    // RabbitMQ targeted headers
    public static readonly string BiUMVersion = "x-bium-version";
    public static readonly string BiAppDomain = "x-biapp-domain";
}