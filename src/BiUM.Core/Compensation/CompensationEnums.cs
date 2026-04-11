namespace BiUM.Core.Compensation;

public enum CompensationSnapshotOperationType
{
    Insert = 0,
    Update = 1,
    Delete = 2
}

public enum CompensationSnapshotRowState
{
    Pending = 0,
    Committed = 1,
    RolledBack = 2,
    Expired = 3
}

public static class CompensationStatusCodes
{
    public const string Insert = "I";
    public const string Update = "U";
    public const string UpdateReadable = "UR";
    public const string Committed = "N";
    public const string Delete = "D";
    public const string DeleteReadable = "DR";

    public static bool IsPendingReadAllowed(string? code) =>
        code is UpdateReadable or DeleteReadable or Committed;

    public static bool IsPendingHiddenFromGlobalRead(string? code) =>
        code is Insert or Update or Delete;
}