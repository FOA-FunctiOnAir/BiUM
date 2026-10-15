using BiUM.Core.Database.Native;
using System;
using System.Collections.Generic;
using System.Data.Common;

namespace BiApp.Test2.Infrastructure.Persistence.Native;

public static class SampleMetricsDatabase
{
    public const string Name = "Metrics";

    public const string Schema = "dbo";

    public static readonly NativeRangePartitionSet Partitions = new(Schema, "SAMPLE_METRIC", NativePartitionInterval.Day);
}

public sealed class M0001SampleMetric : NativeSchemaMigration
{
    public override string Database => SampleMetricsDatabase.Name;

    public override string Id => "0001_sample_metric";

    public override string Sql => """
        CREATE SCHEMA IF NOT EXISTS dbo;

        CREATE TABLE dbo."SAMPLE_METRIC" (
            "SOURCE_ID" integer NOT NULL,
            "MEASURED_AT" timestamptz NOT NULL,
            "VALUE" double precision,
            "VALUES" double precision[] NOT NULL,
            CONSTRAINT "PK_SAMPLE_METRIC" PRIMARY KEY ("SOURCE_ID", "MEASURED_AT")
        ) PARTITION BY RANGE ("MEASURED_AT");
        """;
}

public sealed record SampleMetricRow(int SourceId, DateTime MeasuredAt, double? Value, double?[] Values);

public sealed record SampleMetricMerge : NativeBulkMerge<SampleMetricRow>
{
    public override string Schema => SampleMetricsDatabase.Schema;

    public override string Table => "SAMPLE_METRIC";

    public override IReadOnlyList<NativeBulkColumn<SampleMetricRow>> Columns { get; } =
    [
        new("SOURCE_ID", NativeColumnType.Int32, row => row.SourceId),
        new("MEASURED_AT", NativeColumnType.TimestampTz, row => row.MeasuredAt),
        new("VALUE", NativeColumnType.Double, row => row.Value),
        new("VALUES", NativeColumnType.DoubleArray, row => row.Values)
    ];

    public override IReadOnlyList<string> ConflictColumns { get; } = ["SOURCE_ID", "MEASURED_AT"];

    public override NativeBulkConflictAction ConflictAction => NativeBulkConflictAction.DoUpdate;
}

public sealed record SampleMetricRangeQuery(int SourceId, DateTime From, DateTime To) : NativeSqlQuery<SampleMetricRow>
{
    public override string Sql => """
        SELECT "SOURCE_ID", "MEASURED_AT", "VALUE", "VALUES"
        FROM dbo."SAMPLE_METRIC"
        WHERE "SOURCE_ID" = @source_id AND "MEASURED_AT" >= @from AND "MEASURED_AT" < @to
        ORDER BY "MEASURED_AT"
        """;

    public override IEnumerable<NativeSqlParameter> GetParameters() =>
    [
        new("source_id", SourceId, NativeColumnType.Int32),
        new("from", From, NativeColumnType.TimestampTz),
        new("to", To, NativeColumnType.TimestampTz)
    ];

    public override SampleMetricRow Map(DbDataReader reader) =>
        new(
            reader.GetInt32(0),
            DateTime.SpecifyKind(reader.GetDateTime(1), DateTimeKind.Utc),
            reader.IsDBNull(2) ? null : reader.GetDouble(2),
            reader.GetFieldValue<double?[]>(3));
}

public sealed record DeleteSampleMetricsCommand(int SourceId) : NativeSqlCommand
{
    public override string Sql => """DELETE FROM dbo."SAMPLE_METRIC" WHERE "SOURCE_ID" = @source_id""";

    public override IEnumerable<NativeSqlParameter> GetParameters() =>
    [
        new("source_id", SourceId, NativeColumnType.Int32)
    ];
}