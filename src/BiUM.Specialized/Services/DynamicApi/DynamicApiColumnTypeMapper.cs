namespace BiUM.Specialized.Services.DynamicApi;

internal static class DynamicApiColumnTypeMapper
{
    public static string MapPostgreSql(string dataType, string? udtName)
    {
        var type = (udtName ?? dataType).ToLowerInvariant();

        return type switch
        {
            "uuid" => "System.Guid",
            "int2" or "smallint" => "short",
            "int4" or "integer" => "int",
            "int8" or "bigint" => "long",
            "bool" or "boolean" => "bool",
            "numeric" or "decimal" => "decimal",
            "float4" or "real" => "float",
            "float8" or "double precision" => "double",
            "date" => "System.DateOnly",
            "time" => "System.TimeOnly",
            "timestamp" or "timestamptz" => "System.DateTime",
            "bytea" => "byte[]",
            _ => "string"
        };
    }

    public static string MapSqlServer(string dataType)
    {
        return dataType.ToLowerInvariant() switch
        {
            "uniqueidentifier" => "System.Guid",
            "smallint" => "short",
            "int" => "int",
            "bigint" => "long",
            "bit" => "bool",
            "decimal" or "numeric" or "money" or "smallmoney" => "decimal",
            "real" => "float",
            "float" => "double",
            "date" => "System.DateOnly",
            "time" => "System.TimeOnly",
            "datetime" or "datetime2" or "smalldatetime" or "datetimeoffset" => "System.DateTime",
            "varbinary" or "binary" or "image" => "byte[]",
            _ => "string"
        };
    }
}