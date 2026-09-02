namespace BiUM.Specialized.Services.DynamicApi;

public sealed class DynamicApiColumnMetadata
{
    public required string ColumnName { get; init; }

    public required string StoreType { get; init; }

    public bool IsNullable { get; init; }

    public int? MaxLength { get; init; }

    public required string ClrTypeName { get; init; }
}