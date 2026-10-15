using System.Collections.Generic;
using System.Data.Common;

namespace BiUM.Core.Database.Native;

public abstract record NativeSqlStatement
{
    public abstract string Sql { get; }

    public virtual int? CommandTimeoutSeconds => null;

    public virtual IEnumerable<NativeSqlParameter> GetParameters() => [];
}

public abstract record NativeSqlCommand : NativeSqlStatement;

public abstract record NativeSqlQuery<TResult> : NativeSqlStatement
{
    public abstract TResult Map(DbDataReader reader);
}

public sealed record NativeSqlParameter(string Name, object? Value, NativeColumnType? Type = null);

public enum NativeColumnType
{
    Boolean,
    Int16,
    Int32,
    Int64,
    Double,
    Numeric,
    Text,
    Uuid,
    TimestampTz,
    Date,
    Int16Array,
    Int32Array,
    Int64Array,
    DoubleArray,
    UuidArray,
    TextArray
}