using BiUM.Specialized.Services;
using BiUM.Specialized.Services.Crud;
using BiUM.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace BiUM.Tests.Crud;

public sealed class CrudRuntimeInMemoryTests
{
    [Fact]
    public async Task SaveAsync_returns_crud_not_published_when_no_version_exists()
    {
        await using var sp = CreateProvider("runtime-save-not-published");

        using var scope = sp.CreateScope();
        var crud = scope.ServiceProvider.GetRequiredService<ICrudService>();

        var result = await crud.SaveAsync("MISSING", new Dictionary<string, object?>(), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Messages.Should().Contain(m => m.Code == "crud_not_published");
    }

    [Fact]
    public async Task DeleteAsync_returns_crud_not_published_when_no_version_exists()
    {
        await using var sp = CreateProvider("runtime-delete-not-published");

        using var scope = sp.CreateScope();
        var crud = scope.ServiceProvider.GetRequiredService<ICrudService>();

        var result = await crud.DeleteAsync("MISSING", Guid.NewGuid(), hardDelete: false, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Messages.Should().Contain(m => m.Code == "crud_not_published");
    }

    [Fact]
    public async Task GetListAsync_returns_crud_not_published_when_no_version_exists()
    {
        await using var sp = CreateProvider("runtime-list-not-published");

        using var scope = sp.CreateScope();
        var crud = scope.ServiceProvider.GetRequiredService<ICrudService>();

        var result = await crud.GetListAsync("MISSING", new Dictionary<string, string>(), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Messages.Should().Contain(m => m.Code == "crud_not_published");
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task SavePartialAsync_returns_crud_not_published_when_no_version_exists()
    {
        await using var sp = CreateProvider("runtime-partial-not-published");

        using var scope = sp.CreateScope();
        var crud = scope.ServiceProvider.GetRequiredService<ICrudService>();

        var result = await crud.SavePartialAsync(
            "MISSING",
            "partial",
            Guid.NewGuid(),
            new Dictionary<string, object?>(),
            CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Messages.Should().Contain(m => m.Code == "crud_not_published");
    }

    [Fact]
    public async Task SavePartialAsync_returns_crud_partial_not_found_when_partial_code_unknown()
    {
        var correlation = new TestCorrelationContextProvider
        {
            Context = CorrelationTestHelper.CreateBpmnLike(Guid.NewGuid(), Guid.NewGuid())
        };

        var appId = Guid.NewGuid();
        var msId = Guid.NewGuid();
        const string code = "PARTIAL";

        await using var sp = CreateProvider("runtime-partial-not-found", correlation);

        using (var scope = sp.CreateScope())
        {
            correlation.Context = CorrelationTestHelper.CreateBpmnLike(
                correlation.Context.TenantId!.Value,
                appId,
                correlation.Context.LanguageId);

            var db = scope.ServiceProvider.GetRequiredService<BiUM.Specialized.Database.IDbContext>();
            _ = await CrudTestHelper.SeedPublishedCrudAsync(db, correlation.Context, appId, msId, code);
        }

        using (var scope = sp.CreateScope())
        {
            correlation.Context = CorrelationTestHelper.CreateBpmnLike(
                correlation.Context.TenantId!.Value,
                appId,
                correlation.Context.LanguageId);

            var crud = scope.ServiceProvider.GetRequiredService<ICrudService>();

            var result = await crud.SavePartialAsync(
                code,
                "unknown-partial",
                Guid.NewGuid(),
                new Dictionary<string, object?> { ["Title"] = "x" },
                CancellationToken.None);

            result.Success.Should().BeFalse();
            result.Messages.Should().Contain(m => m.Code == "crud_partial_not_found");
        }
    }

    private static ServiceProvider CreateProvider(string dbName, TestCorrelationContextProvider? correlation = null)
    {
        correlation ??= new TestCorrelationContextProvider
        {
            Context = CorrelationTestHelper.CreateBpmnLike(Guid.NewGuid(), Guid.NewGuid())
        };

        return BiUMServiceFactory.BuildInMemory(correlation, dbName, services =>
        {
            var translationMock = new Mock<ITranslationService>();
            CrudTestHelper.WireTranslationMockToEchoCodes(translationMock);
            services.AddSingleton(translationMock.Object);
        });
    }
}