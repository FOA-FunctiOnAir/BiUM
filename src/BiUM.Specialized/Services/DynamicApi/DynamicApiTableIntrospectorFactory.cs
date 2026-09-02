using System;
using System.Data.Common;

namespace BiUM.Specialized.Services.DynamicApi;

public static class DynamicApiTableIntrospectorFactory
{
    public static IDynamicApiTableIntrospector Create(string databaseType, DbConnection connection)
    {
        if (connection.GetType().FullName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
        {
            return new SqliteDynamicApiTableIntrospector(connection);
        }

        return string.Equals(databaseType, DynamicApiSchemaRules.DbTypePostgresql, StringComparison.OrdinalIgnoreCase)
            ? new PostgreSqlDynamicApiTableIntrospector(connection)
            : new SqlServerDynamicApiTableIntrospector(connection);
    }
}