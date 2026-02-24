namespace BiUM.Core.Constants;

public static class HeaderKeys
{
    public static string ApplicationId = "x-application-id";
    public static string CorrelationContext = "x-correlation-context";
    public static string CorrelationId = "x-correlation-id";
    public static string LanguageId = "x-language-id";
    public static string TenantId = "x-tenant-id";

    public static string ClientIp = "x-client-ip";
    public static string ClientHost = "x-client-host";

    // RabbitMQ targeted headers
    public static string BiUMVersion = "x-bium-version";
    public static string BiAppDomain = "x-biapp-domain";
}