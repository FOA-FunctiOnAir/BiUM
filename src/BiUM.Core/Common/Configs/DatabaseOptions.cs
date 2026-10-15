namespace BiUM.Core.Common.Configs;

public class DatabaseOptions
{
    public const string Name = "Databases";

    public const string DefaultKey = "Default";

    public string Type { get; set; } = DatabaseTypes.PostgreSQL;

    public string? ConnectionString { get; set; }
}

public static class DatabaseTypes
{
    public const string PostgreSQL = "PostgreSQL";

    public const string MSSQL = "MSSQL";

    public const string InMemory = "InMemory";
}