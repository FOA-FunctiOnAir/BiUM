using System;
using System.Collections.Generic;

namespace BiUM.Core.Database.Native;

public abstract record NativeBulkMerge<TRow>
{
    public abstract string Schema { get; }

    public abstract string Table { get; }

    public abstract IReadOnlyList<NativeBulkColumn<TRow>> Columns { get; }

    public abstract IReadOnlyList<string> ConflictColumns { get; }

    public virtual NativeBulkConflictAction ConflictAction => NativeBulkConflictAction.DoNothing;
}

public sealed record NativeBulkColumn<TRow>(string Name, NativeColumnType Type, Func<TRow, object?> Value);

public enum NativeBulkConflictAction
{
    DoNothing,
    DoUpdate
}