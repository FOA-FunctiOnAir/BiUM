using BiUM.Core.Constants;
using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Common.Crud;
using BiUM.Specialized.Common.Models;
using BiUM.Specialized.Database;
using BiUM.Specialized.Services.Crud;
using BiUM.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BiUM.Tests.Crud;

public sealed class CrudCompensatibleFlagTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task IsCrudMutationCompensatibleByCodeAsync_reflects_published_definition(bool compensatible)
    {
        var correlation = new TestCorrelationContextProvider
        {
            Context = CorrelationTestHelper.CreateBpmnLike(Guid.NewGuid(), Guid.NewGuid())
        };

        var appId = Guid.NewGuid();
        var msId = Guid.NewGuid();
        var fieldId = Guid.NewGuid();
        var crudId = Guid.NewGuid();
        var code = compensatible ? "CP1" : "CP2";

        await using var sp = BiUMServiceFactory.BuildInMemory(correlation, Guid.NewGuid().ToString("N"));

        using (var scope = sp.CreateScope())
        {
            correlation.Context = CorrelationTestHelper.CreateBpmnLike(
                correlation.Context.TenantId!.Value,
                appId,
                correlation.Context.LanguageId);

            var crud = scope.ServiceProvider.GetRequiredService<ICrudService>();
            var cmd = new SaveDomainCrudCommand
            {
                Id = crudId,
                ApplicationId = appId,
                MicroserviceId = msId,
                Code = code,
                TableName = "CP_TBL_XXX",
                Compensatible = compensatible,
                NameTr =
                [
                    new BaseEntityTranslationDto
                    {
                        LanguageId = correlation.Context.LanguageId,
                        Translation = "t"
                    }
                ],
                DomainCrudColumns =
                [
                    new SaveDomainCrudCommandColumn
                    {
                        PropertyName = "Title",
                        ColumnName = "TITLE",
                        FieldId = fieldId,
                        DataTypeId = Ids.DataType.String,
                        SortOrder = 0,
                        _rowStatus = RowStatuses.Exist
                    }
                ]
            };

            (await crud.SaveDomainCrudAsync(cmd, CancellationToken.None)).Success.Should().BeTrue();

            var db = scope.ServiceProvider.GetRequiredService<IDbContext>();
            var tenantId = correlation.Context.TenantId!.Value;
            db.DomainCrudVersions.Add(new DomainCrudVersion
            {
                CorrelationId = correlation.Context.CorrelationId,
                TenantId = tenantId,
                ApplicationId = appId,
                CrudId = crudId,
                TableName = cmd.TableName,
                Version = 1,
                Active = true,
                Deleted = false,
                Created = DateOnly.FromDateTime(DateTime.UtcNow),
                CreatedTime = TimeOnly.FromDateTime(DateTime.UtcNow)
            });
            _ = await db.SaveChangesAsync(CancellationToken.None);
        }

        using (var scope = sp.CreateScope())
        {
            correlation.Context = CorrelationTestHelper.CreateBpmnLike(
                correlation.Context.TenantId!.Value,
                appId,
                correlation.Context.LanguageId);

            var crud = scope.ServiceProvider.GetRequiredService<ICrudService>();
            var flag = await crud.IsCrudMutationCompensatibleByCodeAsync(code, CancellationToken.None);
            flag.Should().Be(compensatible);
        }
    }

    [Fact]
    public async Task IsCrudMutationCompensatibleByCodeAsync_false_for_unknown_code()
    {
        var correlation = new TestCorrelationContextProvider
        {
            Context = CorrelationTestHelper.CreateBpmnLike(Guid.NewGuid(), Guid.NewGuid())
        };

        await using var sp = BiUMServiceFactory.BuildInMemory(correlation, Guid.NewGuid().ToString("N"));

        using var scope = sp.CreateScope();
        var crud = scope.ServiceProvider.GetRequiredService<ICrudService>();
        var flag = await crud.IsCrudMutationCompensatibleByCodeAsync("ZZZ", CancellationToken.None);
        flag.Should().BeFalse();
    }
}
