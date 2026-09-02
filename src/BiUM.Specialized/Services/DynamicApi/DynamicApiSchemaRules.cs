using System;
using System.Text.RegularExpressions;

namespace BiUM.Specialized.Services.DynamicApi;

public static partial class DynamicApiSchemaRules
{
    public const string DbTypePostgresql = "PostgreSQL";
    public const string DbTypeSqlServer = "SqlServer";
    public const string DbTypeSqlite = "SQLite";

    [GeneratedRegex("^t_[0-9a-f]{16}_[0-9a-f]{16}$", RegexOptions.IgnoreCase)]
    private static partial Regex TenantCrudSchemaPattern();

    public static string GetCatalogSchema(string databaseType) =>
        string.Equals(databaseType, DbTypePostgresql, StringComparison.OrdinalIgnoreCase) ? "public" : "dbo";

    public static bool IsAllowedSchema(string schema, string databaseType)
    {
        if (string.IsNullOrWhiteSpace(schema))
        {
            return false;
        }

        if (string.Equals(schema, GetCatalogSchema(databaseType), StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(schema, "main", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return TenantCrudSchemaPattern().IsMatch(schema);
    }

    public static bool IsTenantCrudSchema(string schema) => TenantCrudSchemaPattern().IsMatch(schema);

    public static bool IsTableNameValid(string tableName) =>
        !string.IsNullOrWhiteSpace(tableName) && tableName.Length >= 3;

    public static bool IsTableBlocked(string schema, string tableName)
    {
        var upperTable = tableName.ToUpperInvariant();

        if (upperTable.StartsWith("__CRUD", StringComparison.Ordinal)
            || upperTable.StartsWith("__DYNAMIC", StringComparison.Ordinal)
            || upperTable.StartsWith("__COMPENSATION", StringComparison.Ordinal)
            || upperTable.StartsWith("__EF", StringComparison.Ordinal)
            || upperTable.Equals("__TRANSLATION", StringComparison.Ordinal))
        {
            return true;
        }

        return string.Equals(schema, "hangfire", StringComparison.OrdinalIgnoreCase);
    }
}