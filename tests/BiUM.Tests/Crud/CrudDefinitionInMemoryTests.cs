using BiUM.Core.Constants;
using BiUM.Specialized.Common.Crud;
using BiUM.Specialized.Common.Models;
using BiUM.Specialized.Services.Crud;
using BiUM.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BiUM.Tests.Crud;

public sealed class CrudDefinitionInMemoryTests
{
    [Fact]
    public async Task SaveDomainCrud_insert_get_by_code_get_list_edit_delete_unpublished()
    {
        var correlation = new TestCorrelationContextProvider
        {
            Context = CorrelationTestHelper.CreateBpmnLike(
                Guid.NewGuid(),
                Guid.NewGuid())
        };

        var appId = Guid.NewGuid();
        var msId = Guid.NewGuid();
        var fieldId = Guid.NewGuid();
        var crudId = Guid.NewGuid();

        using var sp = BiUMServiceFactory.BuildInMemory(correlation, databaseName: Guid.NewGuid().ToString("N"));

        var cmd = new SaveDomainCrudCommand
        {
            Id = crudId,
            ApplicationId = appId,
            MicroserviceId = msId,
            Code = "MEM",
            TableName = "MEM_CRUD_TBL",
            Compensatible = false,
            NameTr = new List<BaseEntityTranslationDto>
            {
                new()
                {
                    LanguageId = correlation.Context.LanguageId,
                    Translation = "Memory crud"
                }
            },
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

        using (var scope = sp.CreateScope())
        {
            correlation.Context = CorrelationTestHelper.CreateBpmnLike(
                correlation.Context.TenantId!.Value,
                appId,
                correlation.Context.LanguageId);
            var crud = scope.ServiceProvider.GetRequiredService<ICrudService>();
            var r = await crud.SaveDomainCrudAsync(cmd, CancellationToken.None);
            r.Success.Should().BeTrue();
        }

        using (var scope = sp.CreateScope())
        {
            correlation.Context = CorrelationTestHelper.CreateBpmnLike(
                correlation.Context.TenantId!.Value,
                appId,
                correlation.Context.LanguageId);
            var crud = scope.ServiceProvider.GetRequiredService<BiUM.Specialized.Services.Crud.ICrudService>();
            var byCode = await crud.GetDomainCrudByCodeAsync("MEM", CancellationToken.None);
            byCode.Value.Should().NotBeNull();
            byCode.Value!.Code.Should().Be("MEM");
            byCode.Value.TableName.Should().Be("MEM_CRUD_TBL");

            var list = await crud.GetDomainCrudsAsync(appId, null, "MEM", null, 0, 20, CancellationToken.None);
            list.Value.Should().NotBeNull();
            list.Value!.Should().Contain(x => x.Code == "MEM");

            var edit = cmd with
            {
                NameTr = new List<BaseEntityTranslationDto>
                {
                    new()
                    {
                        LanguageId = correlation.Context.LanguageId,
                        Translation = "Memory crud renamed"
                    }
                }
            };
            var r2 = await crud.SaveDomainCrudAsync(edit, CancellationToken.None);
            r2.Success.Should().BeTrue();

            var del = await crud.DeleteDomainCrudAsync(crudId, CancellationToken.None);
            del.Success.Should().BeTrue();
        }

        using (var scope = sp.CreateScope())
        {
            correlation.Context = CorrelationTestHelper.CreateBpmnLike(
                correlation.Context.TenantId!.Value,
                appId,
                correlation.Context.LanguageId);
            var crud = scope.ServiceProvider.GetRequiredService<ICrudService>();
            var gone = await crud.GetDomainCrudAsync(crudId, CancellationToken.None);
            gone.Value.Should().BeNull();
        }
    }
}