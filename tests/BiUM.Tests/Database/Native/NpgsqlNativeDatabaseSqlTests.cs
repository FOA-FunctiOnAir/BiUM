using BiUM.Core.Database.Native;
using BiUM.Specialized.Database.Native;
using FluentAssertions;
using NpgsqlTypes;
using Xunit;

namespace BiUM.Tests.Database.Native;

public sealed class NpgsqlNativeDatabaseSqlTests
{
    private sealed record TestRow(int Id, DateTime At, double?[] Values);

    private sealed record TestMerge(NativeBulkConflictAction Action) : NativeBulkMerge<TestRow>
    {
        public override string Schema => "dbo";

        public override string Table => "SAMPLE";

        public override IReadOnlyList<NativeBulkColumn<TestRow>> Columns { get; } =
        [
            new("ID", NativeColumnType.Int32, row => row.Id),
            new("AT", NativeColumnType.TimestampTz, row => row.At),
            new("VALUES", NativeColumnType.DoubleArray, row => row.Values)
        ];

        public override IReadOnlyList<string> ConflictColumns { get; } = ["ID", "AT"];

        public override NativeBulkConflictAction ConflictAction => Action;
    }

    [Fact]
    public void BuildMergeSql_do_nothing_uses_conflict_columns()
    {
        var sql = NpgsqlNativeDatabase.BuildMergeSql(new TestMerge(NativeBulkConflictAction.DoNothing), "__bulk_x");

        sql.Should().Be("INSERT INTO \"dbo\".\"SAMPLE\" (\"ID\", \"AT\", \"VALUES\") SELECT \"ID\", \"AT\", \"VALUES\" FROM \"__bulk_x\" ON CONFLICT (\"ID\", \"AT\") DO NOTHING");
    }

    [Fact]
    public void BuildMergeSql_do_update_sets_only_non_conflict_columns()
    {
        var sql = NpgsqlNativeDatabase.BuildMergeSql(new TestMerge(NativeBulkConflictAction.DoUpdate), "__bulk_x");

        sql.Should().EndWith("ON CONFLICT (\"ID\", \"AT\") DO UPDATE SET \"VALUES\" = EXCLUDED.\"VALUES\"");
    }

    [Fact]
    public void BuildCreateStagingSql_and_copy_target_the_staging_table()
    {
        var merge = new TestMerge(NativeBulkConflictAction.DoNothing);

        NpgsqlNativeDatabase.BuildCreateStagingSql(merge, "__bulk_x")
            .Should().Be("CREATE TEMP TABLE \"__bulk_x\" (LIKE \"dbo\".\"SAMPLE\" INCLUDING DEFAULTS) ON COMMIT DROP");
        NpgsqlNativeDatabase.BuildCopySql(merge, "__bulk_x")
            .Should().Be("COPY \"__bulk_x\" (\"ID\", \"AT\", \"VALUES\") FROM STDIN (FORMAT BINARY)");
    }

    [Theory]
    [InlineData(NativePartitionInterval.Day, "2026-10-15T13:45:00Z", "READING_20261015", "2026-10-15T00:00:00Z", "2026-10-16T00:00:00Z")]
    [InlineData(NativePartitionInterval.Month, "2026-12-31T23:59:00Z", "READING_202612", "2026-12-01T00:00:00Z", "2027-01-01T00:00:00Z")]
    [InlineData(NativePartitionInterval.Year, "2026-06-15T10:00:00Z", "READING_2026", "2026-01-01T00:00:00Z", "2027-01-01T00:00:00Z")]
    public void Partition_name_and_range_follow_interval(NativePartitionInterval interval, string at, string expectedName, string expectedFrom, string expectedTo)
    {
        var set = new NativeRangePartitionSet("dbo", "READING", interval);
        var start = NpgsqlNativeDatabase.FloorToInterval(DateTime.Parse(at).ToUniversalTime(), interval);

        NpgsqlNativeDatabase.GetPartitionName(set, start).Should().Be(expectedName);
        NpgsqlNativeDatabase.BuildCreatePartitionSql(set, start)
            .Should().Be($"CREATE TABLE IF NOT EXISTS \"dbo\".\"{expectedName}\" PARTITION OF \"dbo\".\"READING\" FOR VALUES FROM ('{expectedFrom}') TO ('{expectedTo}')");
    }

    [Fact]
    public void TryParsePartitionStart_round_trips_generated_names()
    {
        var set = new NativeRangePartitionSet("dbo", "READING", NativePartitionInterval.Day);
        var start = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc);

        var parsed = NpgsqlNativeDatabase.TryParsePartitionStart(set, NpgsqlNativeDatabase.GetPartitionName(set, start));

        parsed.Should().Be(start);
        parsed!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Theory]
    [InlineData("READING_AGGREGATE_R1_202610")]
    [InlineData("READING_default")]
    [InlineData("OTHER_20261015")]
    public void TryParsePartitionStart_ignores_foreign_names(string name)
    {
        var set = new NativeRangePartitionSet("dbo", "READING", NativePartitionInterval.Day);

        NpgsqlNativeDatabase.TryParsePartitionStart(set, name).Should().BeNull();
    }

    [Fact]
    public void AddInterval_handles_month_end()
    {
        NpgsqlNativeDatabase.AddInterval(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), NativePartitionInterval.Month)
            .Should().Be(new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    [Theory]
    [InlineData(NativeColumnType.Int32, NpgsqlDbType.Integer)]
    [InlineData(NativeColumnType.TimestampTz, NpgsqlDbType.TimestampTz)]
    [InlineData(NativeColumnType.DoubleArray, NpgsqlDbType.Array | NpgsqlDbType.Double)]
    [InlineData(NativeColumnType.UuidArray, NpgsqlDbType.Array | NpgsqlDbType.Uuid)]
    public void ToNpgsqlDbType_maps_native_types(NativeColumnType type, NpgsqlDbType expected)
    {
        NpgsqlNativeDatabase.ToNpgsqlDbType(type).Should().Be(expected);
    }

    [Fact]
    public void ToNpgsqlDbType_covers_every_native_type()
    {
        foreach (var type in Enum.GetValues<NativeColumnType>())
        {
            var act = () => NpgsqlNativeDatabase.ToNpgsqlDbType(type);

            act.Should().NotThrow();
        }
    }
}