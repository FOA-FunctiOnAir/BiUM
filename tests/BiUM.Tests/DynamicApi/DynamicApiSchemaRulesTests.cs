using BiUM.Specialized.Services.Crud;
using BiUM.Specialized.Services.DynamicApi;
using FluentAssertions;
using Xunit;

namespace BiUM.Tests.DynamicApi;

public class DynamicApiSchemaRulesTests
{
    [Fact]
    public void ResolveSchema_matches_crud_pattern()
    {
        var appId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var tenantId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var schema = CrudSchemaHelper.ResolveSchema(appId, tenantId);

        DynamicApiSchemaRules.IsTenantCrudSchema(schema).Should().BeTrue();
        DynamicApiSchemaRules.IsAllowedSchema(schema, DynamicApiSchemaRules.DbTypePostgresql).Should().BeTrue();
    }

    [Theory]
    [InlineData("public", true)]
    [InlineData("dbo", false)]
    [InlineData("main", true)]
    [InlineData("random_schema", false)]
    [InlineData("t_notvalid", false)]
    public void IsAllowedSchema_validates_catalog_and_tenant_patterns_for_postgresql(string schema, bool allowed)
    {
        DynamicApiSchemaRules.IsAllowedSchema(schema, DynamicApiSchemaRules.DbTypePostgresql)
            .Should().Be(allowed);
    }

    [Theory]
    [InlineData("dbo", true)]
    [InlineData("public", false)]
    public void IsAllowedSchema_validates_catalog_schema_per_database_type(string schema, bool allowedForSqlServer)
    {
        DynamicApiSchemaRules.IsAllowedSchema(schema, DynamicApiSchemaRules.DbTypeSqlServer)
            .Should().Be(allowedForSqlServer);
    }

    [Theory]
    [InlineData("__CRUD", true)]
    [InlineData("__DYNAMIC_API", true)]
    [InlineData("CUSTOMER", false)]
    public void IsTableBlocked_detects_system_tables(string table, bool blocked)
    {
        DynamicApiSchemaRules.IsTableBlocked("dbo", table).Should().Be(blocked);
    }
}