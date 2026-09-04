using BiApp.Test.Infrastructure.DynamicApi;
using BiUM.Contract.Models.Api;
using BiUM.Core.Constants;
using BiUM.Specialized.Common.API;
using BiUM.Specialized.Common.DynamicApi;
using BiUM.Specialized.Common.Models;
using BiUM.Specialized.Services.DynamicApi;
using BiUM.Specialized.Services.DynamicExporter;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BiApp.Test.API.Controllers;

[BiUMRoute("test")]
public class TestDynamicApiController : ApiControllerBase
{
    private readonly IDynamicApiService _dynamicApiService;
    private readonly IDynamicExporterService _dynamicExporterService;

    public TestDynamicApiController(
        IDynamicApiService dynamicApiService,
        IDynamicExporterService dynamicExporterService)
    {
        _dynamicApiService = dynamicApiService;
        _dynamicExporterService = dynamicExporterService;
    }

    [HttpPost]
    public Task<ApiResponse<SetupCurrencyDynamicApiResult>> SetupCurrencyDynamicApi(CancellationToken cancellationToken) =>
        SetupDynamicApiAsync(
            SampleDynamicApiConstants.CurrencyListApiId,
            SampleDynamicApiConstants.CurrencyListCode,
            "Currency list (simple)",
            SampleDynamicApiHandlerSources.CurrencyList,
            cancellationToken);

    [HttpPost]
    public Task<ApiResponse<SetupCurrencyDynamicApiResult>> SetupCurrencyDynamicApiPaged(CancellationToken cancellationToken) =>
        SetupDynamicApiAsync(
            SampleDynamicApiConstants.CurrencyListPagedApiId,
            SampleDynamicApiConstants.CurrencyListPagedCode,
            "Currency list (manual paging)",
            SampleDynamicApiHandlerSources.CurrencyListPaged,
            cancellationToken);

    [HttpPost]
    public async Task<ApiResponse<SetupCurrencyDynamicApisResult>> SetupCurrencyDynamicApis(CancellationToken cancellationToken)
    {
        var response = new ApiResponse<SetupCurrencyDynamicApisResult>
        {
            Value = new SetupCurrencyDynamicApisResult()
        };

        var getCurrencies = await SetupDynamicApiAsync(
            SampleDynamicApiConstants.CurrencyGetCurrenciesApiId,
            SampleDynamicApiConstants.CurrencyGetCurrenciesCode,
            "Currency list (datagrid)",
            SampleDynamicApiHandlerSources.GetCurrencies,
            cancellationToken);

        if (!getCurrencies.Success)
        {
            response.AddMessage(getCurrencies);
            return response;
        }

        response.Value!.Apis.Add(getCurrencies.Value!);

        var getCurrency = await SetupDynamicApiAsync(
            SampleDynamicApiConstants.CurrencyGetCurrencyApiId,
            SampleDynamicApiConstants.CurrencyGetCurrencyCode,
            "Currency by id",
            SampleDynamicApiHandlerSources.GetCurrency,
            cancellationToken);

        if (!getCurrency.Success)
        {
            response.AddMessage(getCurrency);
            return response;
        }

        response.Value.Apis.Add(getCurrency.Value!);

        return response;
    }

    [HttpPost]
    public async Task<ApiResponse<CreateCurrencyExportResult>> CreateCurrencyExportRequest(CancellationToken cancellationToken)
    {
        var export = await _dynamicExporterService.SaveExportRequestAsync(new SaveExportRequestCommand
        {
            ApplicationId = SampleDynamicApiConstants.ApplicationId,
            SourceMicroserviceId = SampleDynamicApiConstants.MicroserviceId,
            Name = "Currency list export",
            SourceUrl = SampleDynamicApiConstants.CurrencyGetCurrenciesCallUrl,
            SourceParameters = new Dictionary<string, object?>(),
            Format = "xlsx"
        }, cancellationToken);

        if (!export.Success || export.Value is null)
        {
            var response = new ApiResponse<CreateCurrencyExportResult>();
            response.AddMessage(export);
            return response;
        }

        return new ApiResponse<CreateCurrencyExportResult>
        {
            Value = new CreateCurrencyExportResult
            {
                ExportRequestId = export.Value.Id,
                SourceUrl = SampleDynamicApiConstants.CurrencyGetCurrenciesCallUrl,
                StatusPollUrl = $"/api/base/DynamicExporter/GetExportRequest?id={export.Value.Id}",
                DownloadUrl = $"/api/base/DynamicExporter/Download?id={export.Value.Id}"
            }
        };
    }

    private async Task<ApiResponse<SetupCurrencyDynamicApiResult>> SetupDynamicApiAsync(
        Guid apiId,
        string code,
        string displayName,
        string sourceCode,
        CancellationToken cancellationToken)
    {
        var save = await _dynamicApiService.SaveDomainDynamicApiAsync(new SaveDomainDynamicApiCommand
        {
            Id = apiId,
            ApplicationId = SampleDynamicApiConstants.ApplicationId,
            MicroserviceId = SampleDynamicApiConstants.MicroserviceId,
            Code = code,
            NameTr = [new BaseEntityTranslationDto { LanguageId = Ids.Language.English.Id, Translation = displayName }],
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

        var publish = await _dynamicApiService.PublishDomainDynamicApiAsync(apiId, cancellationToken);

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
                ApiId = apiId,
                Code = code,
                CallUrl = $"/api/base/DynamicApi/Get/{code}",
                Mirrors = code switch
                {
                    SampleDynamicApiConstants.CurrencyGetCurrenciesCode => "TestCurrencyController.GetCurrencies / CurrencyRepository.GetCurrencies",
                    SampleDynamicApiConstants.CurrencyGetCurrencyCode => "TestCurrencyController.GetCurrency / CurrencyRepository.GetCurrency",
                    _ => null
                }
            }
        };
    }
}

public class SetupCurrencyDynamicApisResult
{
    public IList<SetupCurrencyDynamicApiResult> Apis { get; set; } = [];
}

public class SetupCurrencyDynamicApiResult
{
    public Guid ApiId { get; set; }

    public required string Code { get; set; }

    public required string CallUrl { get; set; }

    public string? Mirrors { get; set; }
}

public class CreateCurrencyExportResult
{
    public Guid ExportRequestId { get; set; }

    public required string SourceUrl { get; set; }

    public required string StatusPollUrl { get; set; }

    public required string DownloadUrl { get; set; }
}