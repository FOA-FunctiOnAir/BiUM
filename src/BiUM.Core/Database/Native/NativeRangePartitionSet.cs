namespace BiUM.Core.Database.Native;

public sealed record NativeRangePartitionSet(string Schema, string ParentTable, NativePartitionInterval Interval);

public enum NativePartitionInterval
{
    Day,
    Month,
    Year
}