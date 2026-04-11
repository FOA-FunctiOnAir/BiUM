using BiUM.Specialized.Database;
using Microsoft.AspNetCore.Http;
using System;

namespace BiUM.Specialized.Middlewares;

public static class RequestTransactionMiddlewarePolicies
{
    public static bool ShouldSkipTransaction(HttpContext context)
    {
        if (context.Request.ContentType == "application/grpc")
        {
            return true;
        }

        var method = context.Request.Method;

        if (HttpMethods.IsGet(method) || HttpMethods.IsOptions(method) || HttpMethods.IsHead(method))
        {
            return true;
        }

        var path = context.Request.Path;

        if (path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/version", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    public static bool IsInMemoryDatabaseProvider(IDbContext dbContext)
    {
        return dbContext.Database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) == true;
    }
}