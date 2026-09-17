using BiApp.Test.Infrastructure.Crud;
using BiApp.Test.Infrastructure.DynamicApi;
using BiUM.Contract.Models.Api;
using BiUM.Core.Authorization;
using BiUM.Core.Constants;
using BiUM.Specialized.Common.API;
using BiUM.Specialized.Common.Crud;
using BiUM.Specialized.Common.DynamicApi;
using BiUM.Specialized.Common.Models;
using BiUM.Specialized.Services.Crud;
using BiUM.Specialized.Services.DynamicApi;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BiApp.Test.API.Controllers;

[BiUMRoute("test")]
public class TestCrudDynamicApiController : ApiControllerBase
{
    private readonly ICrudService _crudService;
    private readonly IDynamicApiService _dynamicApiService;
    private readonly ICorrelationContextProvider _correlationContextProvider;
    private readonly ICorrelationContextAccessor _correlationContextAccessor;

    public TestCrudDynamicApiController(
        ICrudService crudService,
        IDynamicApiService dynamicApiService,
        ICorrelationContextProvider correlationContextProvider,
        ICorrelationContextAccessor correlationContextAccessor)
    {
        _crudService = crudService;
        _dynamicApiService = dynamicApiService;
        _correlationContextProvider = correlationContextProvider;
        _correlationContextAccessor = correlationContextAccessor;
    }

    [HttpPost]
    public Task<ApiResponse<SetupSampleNotesCrudResult>> SetupSampleNotesCrud(CancellationToken cancellationToken)
    {
        var context = SampleCrudContextHelper.Ensure(_correlationContextAccessor, _correlationContextProvider);

        return SetupCrudAsync(context, cancellationToken);
    }

    [HttpPost]
    public Task<ApiResponse<SetupCurrencyDynamicApiResult>> SetupSampleNotesDynamicApi(CancellationToken cancellationToken)
    {
        var context = SampleCrudContextHelper.Ensure(_correlationContextAccessor, _correlationContextProvider);

        return SetupDynamicApiAsync(context, cancellationToken);
    }

    [HttpPost]
    public async Task<ApiResponse<SetupSampleNotesPipelineResult>> SetupSampleNotesCrudAndDynamicApi(CancellationToken cancellationToken)
    {
        var context = SampleCrudContextHelper.Ensure(_correlationContextAccessor, _correlationContextProvider);
        var response = new ApiResponse<SetupSampleNotesPipelineResult>();

        var crud = await SetupCrudAsync(context, cancellationToken);

        if (!crud.Success || crud.Value is null)
        {
            response.AddMessage(crud);

            return response;
        }

        var api = await SetupDynamicApiAsync(context, cancellationToken);

        if (!api.Success || api.Value is null)
        {
            response.AddMessage(api);

            return response;
        }

        var seedTitles = new[] { "First sample note", "Second sample note" };

        foreach (var title in seedTitles)
        {
            var seed = await _crudService.SaveAsync(
                SampleCrudConstants.NotesCrudCode,
                new Dictionary<string, object?> { ["Title"] = title },
                cancellationToken);

            if (!seed.Success)
            {
                response.AddMessage(seed);

                return response;
            }
        }

        response.Value = new SetupSampleNotesPipelineResult
        {
            Crud = crud.Value,
            DynamicApi = api.Value,
            CrudSaveUrl = $"/api/base/Crud/Save/{SampleCrudConstants.NotesCrudCode}",
            CrudGetListUrl = $"/api/base/Crud/GetList/{SampleCrudConstants.NotesCrudCode}"
        };

        return response;
    }

    private async Task<ApiResponse<SetupSampleNotesCrudResult>> SetupCrudAsync(
        SampleCrudContextHelper.SampleContext context,
        CancellationToken cancellationToken)
    {
        var save = await _crudService.SaveDomainCrudAsync(new SaveDomainCrudCommand
        {
            Id = SampleCrudConstants.NotesCrudId,
            ApplicationId = context.ApplicationId,
            MicroserviceId = SampleDynamicApiConstants.MicroserviceId,
            Code = SampleCrudConstants.NotesCrudCode,
            TableName = SampleCrudConstants.NotesTableName,
            Compensatible = false,
            NameTr =
            [
                new BaseEntityTranslationDto
                {
                    LanguageId = Ids.Language.English.Id,
                    Translation = "Sample notes"
                }
            ],
            DomainCrudColumns =
            [
                new SaveDomainCrudCommandColumn
                {
                    PropertyName = "Title",
                    ColumnName = "TITLE",
                    FieldId = SampleCrudConstants.TitleFieldId,
                    DataTypeId = Ids.DataType.String,
                    SortOrder = 0,
                    _rowStatus = RowStatuses.Exist
                }
            ]
        }, cancellationToken);

        if (!save.Success)
        {
            var response = new ApiResponse<SetupSampleNotesCrudResult>();
            response.AddMessage(save);

            return response;
        }

        var publish = await _crudService.PublishDomainCrudAsync(SampleCrudConstants.NotesCrudId, cancellationToken);

        if (!publish.Success)
        {
            var response = new ApiResponse<SetupSampleNotesCrudResult>();
            response.AddMessage(publish);

            return response;
        }

        return new ApiResponse<SetupSampleNotesCrudResult>
        {
            Value = new SetupSampleNotesCrudResult
            {
                CrudId = SampleCrudConstants.NotesCrudId,
                Code = SampleCrudConstants.NotesCrudCode,
                TableName = SampleCrudConstants.NotesTableName,
                Schema = context.Schema,
                TenantId = context.TenantId,
                ApplicationId = context.ApplicationId
            }
        };
    }

    private async Task<ApiResponse<SetupCurrencyDynamicApiResult>> SetupDynamicApiAsync(
        SampleCrudContextHelper.SampleContext context,
        CancellationToken cancellationToken)
    {
        var sourceCode = SampleCrudHandlerSources.BuildNotesList(context.Schema);

        var save = await _dynamicApiService.SaveDomainDynamicApiAsync(new SaveDomainDynamicApiCommand
        {
            Id = SampleCrudConstants.NotesListApiId,
            ApplicationId = context.ApplicationId,
            TenantId = context.TenantId,
            MicroserviceId = SampleDynamicApiConstants.MicroserviceId,
            Code = SampleCrudConstants.NotesListApiCode,
            NameTr =
            [
                new BaseEntityTranslationDto
                {
                    LanguageId = Ids.Language.English.Id,
                    Translation = "Sample notes list (CRUD table)"
                }
            ],
            HttpType = Ids.Parameter.HttpType.Values.Get,
            ExecutionType = Ids.Parameter.DynamicApiExecutionType.Values.CSharpEf,
            RuntimePlatformType = SampleDynamicApiConstants.RuntimePlatformType,
            SourceCode = sourceCode,
            Compensatible = false
        }, cancellationToken);

        if (!save.Success)
        {
            var response = new ApiResponse<SetupCurrencyDynamicApiResult>();
            response.AddMessage(save);

            return response;
        }

        var publish = await _dynamicApiService.PublishDomainDynamicApiAsync(SampleCrudConstants.NotesListApiId, cancellationToken);

        if (!publish.Success)
        {
            var response = new ApiResponse<SetupCurrencyDynamicApiResult>();
            response.AddMessage(publish);

            return response;
        }

        return new ApiResponse<SetupCurrencyDynamicApiResult>
        {
            Value = new SetupCurrencyDynamicApiResult
            {
                ApiId = SampleCrudConstants.NotesListApiId,
                Code = SampleCrudConstants.NotesListApiCode,
                CallUrl = SampleCrudConstants.NotesListCallUrl,
                Mirrors = $"DomainCrud {SampleCrudConstants.NotesCrudCode} / {context.Schema}.{SampleCrudConstants.NotesTableName}"
            }
        };
    }
}

public class SetupSampleNotesCrudResult
{
    public Guid CrudId { get; set; }
    public required string Code { get; set; }
    public required string TableName { get; set; }
    public required string Schema { get; set; }
    public Guid TenantId { get; set; }
    public Guid ApplicationId { get; set; }
}

public class SetupSampleNotesPipelineResult
{
    public SetupSampleNotesCrudResult Crud { get; set; } = null!;
    public SetupCurrencyDynamicApiResult DynamicApi { get; set; } = null!;
    public required string CrudSaveUrl { get; set; }
    public required string CrudGetListUrl { get; set; }
}