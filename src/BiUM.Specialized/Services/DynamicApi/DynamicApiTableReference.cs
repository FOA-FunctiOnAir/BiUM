namespace BiUM.Specialized.Services.DynamicApi;

public sealed record DynamicApiTableReference(string Schema, string TableName)
{
    public string Key => $"{Schema}.{TableName}";
}