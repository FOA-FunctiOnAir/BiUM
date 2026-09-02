using BiUM.Specialized.Services.DynamicApi;
using FluentAssertions;
using Xunit;

namespace BiUM.Tests.DynamicApi;

public class DynamicApiEntityCodegenTests
{
    [Fact]
    public void GenerateEntitySource_emits_table_and_columns()
    {
        var reference = new DynamicApiTableReference("dbo", "Product");
        var columns = new List<DynamicApiColumnMetadata>
        {
            new() { ColumnName = "ID", StoreType = "uuid", IsNullable = false, ClrTypeName = "System.Guid" },
            new() { ColumnName = "NAME", StoreType = "text", IsNullable = true, ClrTypeName = "string" }
        };

        var source = DynamicApiEntityCodegen.GenerateEntitySource(reference, columns);

        source.Should().Contain("class DynamicEntity_dbo_Product");
        source.Should().Contain("[Table(\"Product\", Schema = \"dbo\")]");
        source.Should().Contain("public System.Guid Id");
        source.Should().Contain("public string? Name");
    }

    [Fact]
    public void GenerateDbContextSource_maps_all_entities()
    {
        var references = new[]
        {
            new DynamicApiTableReference("dbo", "A"),
            new DynamicApiTableReference("dbo", "B")
        };

        var source = DynamicApiEntityCodegen.GenerateDbContextSource(references);

        source.Should().Contain("DynamicEntity_dbo_A");
        source.Should().Contain("DynamicEntity_dbo_B");
    }

    [Fact]
    public void ToEntityTypeName_sanitizes_identifiers()
    {
        DynamicApiEntityCodegen.ToEntityTypeName("dbo", "MY-TABLE")
            .Should().Be("DynamicEntity_dbo_MY_TABLE");
    }
}

public class DynamicApiSourceTransformerTests
{
    [Fact]
    public void Transform_replaces_entity_calls_with_dbset()
    {
        const string source = """
            var rows = await ctx.Entity("dbo", "Product").ToListAsync(cancellationToken);
            """;

        var references = new[] { new DynamicApiTableReference("dbo", "Product") };
        var transformed = DynamicApiSourceTransformer.Transform(source, references);

        transformed.Should().Contain("__dynamicTables.Set<DynamicEntity_dbo_Product>()");
        transformed.Should().NotContain("ctx.Entity");
    }
}