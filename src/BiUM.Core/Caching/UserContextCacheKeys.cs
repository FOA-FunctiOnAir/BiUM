using System;

namespace BiUM.Core.Caching;

public static class UserContextCacheKeys
{
    public const string GatewayPrefix = "jwt:user-credential:";

    public const string CustomersGetFwMePrefix = "customers:fw-me:";

    public const string AuthMePrefix = "auth:me:";

    public static string BuildCustomersGetFwMeKey(Guid applicationId, Guid tenantId, Guid customerId, Guid languageId) =>
        $"{CustomersGetFwMePrefix}{applicationId}:{tenantId}:{customerId}:{languageId}";

    public static string BuildAuthMeKey(Guid applicationId, Guid tenantId, Guid customerId, Guid languageId, int? themeOverrideVersion) =>
        $"{AuthMePrefix}{applicationId}:{tenantId}:{customerId}:{languageId}:{themeOverrideVersion?.ToString() ?? "null"}";
}