using BiUM.Contract.Models.Api;
using BiUM.Specialized.Services.DynamicApi;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Moq;
using System.Text.Json;
using Xunit;

namespace BiUM.Tests.DynamicApi;

public sealed class CrudTableDynamicApiSqliteTests
{
    [Fact]
    public async Task Execute_reads_crud_shaped_table_rows_via_entity_pipeline()
    {
        const string connectionString = "Data Source=CrudTableDynamicApiExecuteTest;Mode=Memory;Cache=Shared";
        const string tableName = "SAMPLE_NOTES";

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = $$"""
                CREATE TABLE "{{tableName}}" (
                    "ID" TEXT NOT NULL PRIMARY KEY,
                    "CORRELATION_ID" TEXT NOT NULL,
                    "TENANT_ID" TEXT NOT NULL,
                    "ACTIVE" INTEGER NOT NULL DEFAULT 1,
                    "DELETED" INTEGER NOT NULL DEFAULT 0,
                    "CREATED" TEXT NOT NULL,
                    "CREATED_TIME" TEXT NOT NULL,
                    "TEST" INTEGER NOT NULL DEFAULT 0,
                    "TITLE" TEXT NULL
                );
                INSERT INTO "{{tableName}}" ("ID", "CORRELATION_ID", "TENANT_ID", "ACTIVE", "DELETED", "CREATED", "CREATED_TIME", "TEST", "TITLE")
                VALUES ('11111111-1111-1111-1111-111111111111', '22222222-2222-2222-2222-222222222222', '33333333-3333-3333-3333-333333333333', 1, 0, '2026-01-01', '00:00:00', 0, 'Alpha');
                """;
            await cmd.ExecuteNonQueryAsync();
        }

        const string source = """
            var items = await ctx.Entity("main", "SAMPLE_NOTES")
                .Select(x => new { x.Id, x.Title })
                .ToListAsync(cancellationToken);
            return new BiUM.Contract.Models.Api.ApiResponse<object> { Value = items };
            """;

        var references = DynamicApiEntityParser.Parse(source).TableReferences;
        var introspector = DynamicApiTableIntrospectorFactory.Create(DynamicApiSchemaRules.DbTypeSqlite, connection);
        var columns = await introspector.GetColumnsAsync("main", tableName, CancellationToken.None);
        var additionalSources = new List<string>
        {
            DynamicApiEntityCodegen.GenerateEntitySource(references[0], columns),
            DynamicApiEntityCodegen.GenerateDbContextSource([(references[0], columns)]),
            DynamicApiEntityCodegen.GenerateDbContextFactorySource()
        };

        var transformed = DynamicApiSourceTransformer.Transform(source, references);
        var compile = DynamicApiCompiler.Compile(new DynamicApiCompileRequest
        {
            HandlerSourceCode = transformed,
            TypeNameSeed = "crud-notes-exec",
            UsesDynamicTables = true,
            AdditionalSourceFiles = additionalSources
        });

        compile.Success.Should().BeTrue(compile.Error);

        var cache = new DynamicApiRuntimeCache(new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()));
        var handler = cache.GetOrLoad("crud-notes", compile.AssemblyBytes!, compile.EntryPointTypeName!);

        var ctx = DynamicApiExecutionContextTestHelper.Create(
            Mock.Of<BiUM.Specialized.Database.IDbContext>(),
            connectionString: connectionString,
            databaseType: DynamicApiSchemaRules.DbTypeSqlite);

        var result = await handler.ExecuteAsync(ctx, CancellationToken.None);

        var response = result.Should().BeOfType<ApiResponse<object>>().Subject;
        response.Value.Should().NotBeNull();
        JsonSerializer.Serialize(response.Value).Should().Contain("Alpha");
    }
}