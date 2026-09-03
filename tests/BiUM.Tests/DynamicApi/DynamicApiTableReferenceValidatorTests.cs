using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Services.Crud;
using BiUM.Specialized.Services.DynamicApi;
using BiUM.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BiUM.Tests.DynamicApi;

public class DynamicApiTableReferenceValidatorTests
{
    [Fact]
    public async Task ValidateAsync_allows_matching_crud_app_and_tenant()
    {
        var appId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var msId = Guid.NewGuid();
        var schema = CrudSchemaHelper.ResolveSchema(appId, tenantId);

        await using var provider = BuildProvider("validator-match");
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TestBiDbContext>();

        db.DomainCruds.Add(new DomainCrud
        {
            ApplicationId = appId,
            TenantId = tenantId,
            MicroserviceId = msId,
            Name = "Test",
            Code = "test-crud",
            TableName = "MY_TABLE"
        });
        await db.SaveChangesAsync();

        var result = await DynamicApiTableReferenceValidator.ValidateAsync(
            db,
            DynamicApiSchemaRules.DbTypePostgresql,
            appId,
            msId,
            tenantId,
            correlationTenantId: tenantId,
            [new DynamicApiTableReference(schema, "MY_TABLE")],
            requirePhysicalTable: false,
            introspector: null,
            CancellationToken.None);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_rejects_crud_table_with_mismatched_tenant()
    {
        var appId = Guid.NewGuid();
        var crudTenantId = Guid.NewGuid();
        var apiTenantId = Guid.NewGuid();
        var msId = Guid.NewGuid();
        var schema = CrudSchemaHelper.ResolveSchema(appId, crudTenantId);

        await using var provider = BuildProvider("validator-mismatch");
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TestBiDbContext>();

        db.DomainCruds.Add(new DomainCrud
        {
            ApplicationId = appId,
            TenantId = crudTenantId,
            MicroserviceId = msId,
            Name = "Test",
            Code = "test-crud",
            TableName = "MY_TABLE"
        });
        await db.SaveChangesAsync();

        var result = await DynamicApiTableReferenceValidator.ValidateAsync(
            db,
            DynamicApiSchemaRules.DbTypePostgresql,
            appId,
            msId,
            apiTenantId,
            correlationTenantId: apiTenantId,
            [new DynamicApiTableReference(schema, "MY_TABLE")],
            requirePhysicalTable: false,
            introspector: null,
            CancellationToken.None);

        result.Errors.Should().Contain("dynamic_api_crud_table_tenant_mismatch");
    }

    [Fact]
    public async Task ValidateAsync_rejects_unknown_tenant_schema()
    {
        await using var provider = BuildProvider("validator-unknown");
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TestBiDbContext>();

        var result = await DynamicApiTableReferenceValidator.ValidateAsync(
            db,
            DynamicApiSchemaRules.DbTypePostgresql,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            [new DynamicApiTableReference("t_a1b2c3d4e5f67890_b2c3d4e5f6789012", "MISSING")],
            requirePhysicalTable: false,
            introspector: null,
            CancellationToken.None);

        result.Errors.Should().Contain("dynamic_api_crud_table_not_found");
    }

    private static ServiceProvider BuildProvider(string dbName)
    {
        var correlation = new TestCorrelationContextProvider();
        return BiUMServiceFactory.BuildInMemory(correlation, dbName);
    }
}