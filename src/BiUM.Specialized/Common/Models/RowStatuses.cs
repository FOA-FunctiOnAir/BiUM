using System.Collections.Generic;

namespace BiUM.Specialized.Common.Models;

public static class RowStatuses
{
    public const int Exist = 0;
    public const int New = 1;
    public const int Edited = 2;
    public const int Deleted = 3;

    public static readonly IReadOnlySet<int> All
        = new HashSet<int>
        {
            Exist,
            New,
            Edited,
            Deleted
        };
}
