using BiUM.Core.Constants;
using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Common.DynamicApi;
using BiUM.Specialized.Common.Models;
using BiUM.Specialized.Database;
using BiUM.Specialized.Services.Crud;
using BiUM.Specialized.Services.DynamicApi;
using BiUM.Tests.Helpers;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace BiUM.Tests.DynamicApi;

public class DynamicApiEntityCompileTests
{
    [Fact]
    public void Compile_with_entity_codegen_succeeds()
    {
        const string userSource = """
            var count = await __dynamicTables.Set<DynamicEntity_main_Sample>().CountAsync(cancellationToken);
            return new BiUM.Contract.Models.Api.ApiResponse<int> { Value = count };
            """;

        var reference = new DynamicApiTableReference("main", "Sample");
        var columns = new List<DynamicApiColumnMetadata>
        {
            new() { ColumnName = "ID", StoreType = "INTEGER", IsNullable = false, ClrTypeName = "long" },
            new() { ColumnName = "NAME", StoreType = "TEXT", IsNullable = true, ClrTypeName = "string" }
        };

        var additionalSources = new List<string>
        {
            DynamicApiEntityCodegen.GenerateEntitySource(reference, columns),
            DynamicApiEntityCodegen.GenerateDbContextSource([reference]),
            DynamicApiEntityCodegen.GenerateDbContextFactorySource()
        };

        var result = DynamicApiCompiler.Compile(new DynamicApiCompileRequest
        {
            HandlerSourceCode = userSource,
            TypeNameSeed = "entity-compile",
            UsesDynamicTables = true,
            AdditionalSourceFiles = additionalSources
        });

        result.Success.Should().BeTrue(result.Error);
        result.AssemblyBytes.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Full_pipeline_transform_compile_and_execute_with_sqlite()
    {
        const string connectionString = "Data Source=DynamicApiEntityPipelineTest;Mode=Memory;Cache=Shared";
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = """
                CREATE TABLE Sample (
                    ID INTEGER PRIMARY KEY,
                    NAME TEXT NULL
                );
                INSERT INTO Sample (ID, NAME) VALUES (1, 'one'), (2, 'two');
                """;
            await cmd.ExecuteNonQueryAsync();
        }

        const string source = """
            return new BiUM.Contract.Models.Api.ApiResponse<int>
            {
                Value = await ctx.Entity("main", "Sample").CountAsync(cancellationToken)
            };
            """;

        var references = DynamicApiEntityParser.Parse(source).TableReferences;
        var introspector = DynamicApiTableIntrospectorFactory.Create(DynamicApiSchemaRules.DbTypeSqlite, connection);
        var columns = await introspector.GetColumnsAsync("main", "Sample", CancellationToken.None);
        var additionalSources = new List<string>
        {
            DynamicApiEntityCodegen.GenerateEntitySource(references[0], columns),
            DynamicApiEntityCodegen.GenerateDbContextSource(references),
            DynamicApiEntityCodegen.GenerateDbContextFactorySource()
        };

        var transformed = DynamicApiSourceTransformer.Transform(source, references);
        var compile = DynamicApiCompiler.Compile(new DynamicApiCompileRequest
        {
            HandlerSourceCode = transformed,
            TypeNameSeed = "sqlite-exec",
            UsesDynamicTables = true,
            AdditionalSourceFiles = additionalSources
        });

        compile.Success.Should().BeTrue(compile.Error);

        var cache = new DynamicApiRuntimeCache(new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()));
        var handler = cache.GetOrLoad("test", compile.AssemblyBytes!, compile.EntryPointTypeName!);

        var ctx = new DynamicApiExecutionContext(
            Mock.Of<IDbContext>(),
            new Dictionary<string, object?>(),
            null,
            null,
            null,
            connectionString,
            DynamicApiSchemaRules.DbTypeSqlite);

        var result = await handler.ExecuteAsync(ctx, CancellationToken.None);

        ((BiUM.Contract.Models.Api.ApiResponse<int>)result).Value.Should().Be(2);
    }
}

public class DynamicApiServiceIntegrationTests
{
    private static readonly Guid TestRuntimePlatformType = Guid.Parse("01a05cfc-0000-7000-8000-000000000001");

    [Fact]
    public async Task SaveDomainDynamicApi_syncs_table_registry()
    {
        var appId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var msId = Guid.NewGuid();
        var schema = CrudSchemaHelper.ResolveSchema(appId, tenantId);

        await using var provider = BuildDynamicApiProvider("save-sync", tenantId, appId);
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TestBiDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IDynamicApiService>();

        db.DomainCruds.Add(new DomainCrud
        {
            ApplicationId = appId,
            TenantId = tenantId,
            MicroserviceId = msId,
            Name = "Crud",
            Code = "crud-code",
            TableName = "ORDERS"
        });
        await db.SaveChangesAsync();

        var apiId = Guid.NewGuid();
        var save = await service.SaveDomainDynamicApiAsync(new SaveDomainDynamicApiCommand
        {
            Id = apiId,
            ApplicationId = appId,
            MicroserviceId = msId,
            Code = "get-orders",
            NameTr = [new BaseEntityTranslationDto { LanguageId = Ids.Language.English.Id, Translation = "Get Orders" }],
            HttpType = Ids.Parameter.HttpType.Values.Get,
            ExecutionType = Ids.Parameter.DynamicApiExecutionType.Values.CSharpEf,
            RuntimePlatformType = TestRuntimePlatformType,
            SourceCode = $$"""return new BiUM.Contract.Models.Api.ApiResponse();""",
            Compensatible = false
        }, CancellationToken.None);

        save.Success.Should().BeTrue(string.Join(", ", save.Messages.Select(m => m.Code)));

        var withEntity = await service.SaveDomainDynamicApiAsync(new SaveDomainDynamicApiCommand
        {
            Id = apiId,
            ApplicationId = appId,
            MicroserviceId = msId,
            Code = "get-orders",
            NameTr = [new BaseEntityTranslationDto { LanguageId = Ids.Language.English.Id, Translation = "Get Orders" }],
            HttpType = Ids.Parameter.HttpType.Values.Get,
            ExecutionType = Ids.Parameter.DynamicApiExecutionType.Values.CSharpEf,
            RuntimePlatformType = TestRuntimePlatformType,
            SourceCode = $$"""
                _ = ctx.Entity("{{schema}}", "ORDERS");
                return new BiUM.Contract.Models.Api.ApiResponse();
                """,
            Compensatible = false
        }, CancellationToken.None);

        withEntity.Success.Should().BeTrue(string.Join(", ", withEntity.Messages.Select(m => m.Code)));

        var tables = await db.DomainDynamicApiTables.Where(t => t.DynamicApiId == apiId).ToListAsync();
        tables.Should().ContainSingle(t => t.Schema == schema && t.TableName == "ORDERS");
    }

    [Fact]
    public async Task GetDomainDynamicApisByTable_returns_linked_apis()
    {
        var appId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var msId = Guid.NewGuid();
        var schema = "dbo";

        await using var provider = BuildDynamicApiProvider("by-table", tenantId, appId);
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TestBiDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IDynamicApiService>();

        var api = new DomainDynamicApi
        {
            Id = Guid.NewGuid(),
            ApplicationId = appId,
            TenantId = tenantId,
            MicroserviceId = msId,
            Name = "Api",
            Code = "linked-api",
            HttpType = Ids.Parameter.HttpType.Values.Get,
            ExecutionType = Ids.Parameter.DynamicApiExecutionType.Values.CSharpEf,
            RuntimePlatformType = TestRuntimePlatformType,
            SourceCode = "return new BiUM.Contract.Models.Api.ApiResponse();"
        };

        db.DomainDynamicApis.Add(api);
        db.DomainDynamicApiTables.Add(new DomainDynamicApiTable
        {
            DynamicApiId = api.Id,
            Schema = schema,
            TableName = "Product"
        });
        await db.SaveChangesAsync();

        var result = await service.GetDomainDynamicApisByTableAsync(schema, "Product", CancellationToken.None);

        result.Value.Should().ContainSingle(x => x.Code == "linked-api");
    }

    [Fact]
    public async Task DeleteDomainCrud_is_blocked_when_dynamic_api_uses_table()
    {
        var appId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var msId = Guid.NewGuid();
        var schema = CrudSchemaHelper.ResolveSchema(appId, tenantId);

        await using var provider = BuildDynamicApiProvider("crud-delete-block", tenantId, appId);
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TestBiDbContext>();
        var crudService = scope.ServiceProvider.GetRequiredService<ICrudService>();

        var crudId = Guid.NewGuid();
        db.DomainCruds.Add(new DomainCrud
        {
            Id = crudId,
            ApplicationId = appId,
            TenantId = tenantId,
            MicroserviceId = msId,
            Name = "Crud",
            Code = "blocked-crud",
            TableName = "BLOCKED"
        });

        var api = new DomainDynamicApi
        {
            Id = Guid.NewGuid(),
            ApplicationId = appId,
            TenantId = tenantId,
            MicroserviceId = msId,
            Name = "Api",
            Code = "uses-blocked",
            HttpType = Ids.Parameter.HttpType.Values.Get,
            ExecutionType = Ids.Parameter.DynamicApiExecutionType.Values.CSharpEf,
            RuntimePlatformType = TestRuntimePlatformType,
            SourceCode = "return new BiUM.Contract.Models.Api.ApiResponse();"
        };

        db.DomainDynamicApis.Add(api);
        db.DomainDynamicApiTables.Add(new DomainDynamicApiTable
        {
            DynamicApiId = api.Id,
            Schema = schema,
            TableName = "BLOCKED"
        });
        await db.SaveChangesAsync();

        var delete = await crudService.DeleteDomainCrudAsync(crudId, CancellationToken.None);

        delete.Success.Should().BeFalse();
        delete.Messages.Should().Contain(m => m.Code == "crud_delete_blocked_by_dynamic_api_codes");
        delete.Messages.Should().Contain(m => m.Message == "uses-blocked");
    }

    private static ServiceProvider BuildDynamicApiProvider(string dbName, Guid tenantId, Guid appId)
    {
        var correlation = new TestCorrelationContextProvider
        {
            Context = CorrelationTestHelper.CreateBpmnLike(tenantId, appId)
        };

        return BiUMServiceFactory.BuildInMemory(correlation, dbName, services =>
        {
            services.AddMemoryCache();
            services.AddSingleton<DynamicApiRuntimeCache>();
            services.AddScoped<IDynamicApiService, DynamicApiService>();
        });
    }
}