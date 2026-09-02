using Microsoft.EntityFrameworkCore;
using System;

namespace BiUM.Specialized.Services.DynamicApi;

public static class DynamicApiDbContextOptions
{
    public static void Configure(DbContextOptionsBuilder builder, string databaseType, string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("dynamic_api_connection_string_missing");
        }

        if (string.Equals(databaseType, DynamicApiSchemaRules.DbTypePostgresql, StringComparison.OrdinalIgnoreCase))
        {
            builder.UseNpgsql(connectionString);
            return;
        }

        if (string.Equals(databaseType, DynamicApiSchemaRules.DbTypeSqlite, StringComparison.OrdinalIgnoreCase))
        {
            builder.UseSqlite(connectionString);
            return;
        }

        builder.UseSqlServer(connectionString);
    }
}