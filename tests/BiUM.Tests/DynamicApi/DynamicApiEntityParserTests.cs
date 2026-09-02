using BiUM.Specialized.Services.DynamicApi;
using FluentAssertions;
using Xunit;

namespace BiUM.Tests.DynamicApi;

public class DynamicApiEntityParserTests
{
    [Fact]
    public void Parse_extracts_literal_entity_references()
    {
        const string source = """
            var q = ctx.Entity("dbo", "Product");
            var q2 = ctx.Entity("t_a1b2c3d4e5f67890_b2c3d4e5f6789012", "MY_TABLE");
            """;

        var result = DynamicApiEntityParser.Parse(source);

        result.Errors.Should().BeEmpty();
        result.TableReferences.Should().HaveCount(2);
        result.TableReferences.Should().Contain(r => r.Schema == "dbo" && r.TableName == "Product");
    }

    [Fact]
    public void Parse_deduplicates_same_table()
    {
        const string source = """
            _ = ctx.Entity("dbo", "Product");
            _ = ctx.Entity("dbo", "Product");
            """;

        var result = DynamicApiEntityParser.Parse(source);

        result.TableReferences.Should().HaveCount(1);
    }

    [Fact]
    public void Parse_rejects_non_literal_arguments()
    {
        const string source = """
            var schema = "dbo";
            _ = ctx.Entity(schema, "Product");
            """;

        var result = DynamicApiEntityParser.Parse(source);

        result.Errors.Should().Contain("dynamic_api_entity_literal_required");
    }

    [Fact]
    public void Parse_rejects_wrong_receiver()
    {
        const string source = """db.Entity("dbo", "Product");""";

        var result = DynamicApiEntityParser.Parse(source);

        result.Errors.Should().Contain("dynamic_api_entity_literal_required");
    }
}