using BiApp.Test.Infrastructure.DynamicApi;
using BiUM.Contract.Models.Api;
using BiUM.Core.Constants;
using BiUM.Specialized.Common.API;
using BiUM.Specialized.Common.DynamicApi;
using BiUM.Specialized.Common.Models;
using BiUM.Specialized.Services.DynamicApi;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiApp.Test.API.Controllers;

[BiUMRoute("test")]
public class TestDynamicApiController : ApiControllerBase
{
    private readonly IDynamicApiService _dynamicApiService;

    public TestDynamicApiController(IDynamicApiService dynamicApiService)
    {
        _dynamicApiService = dynamicApiService;
    }

    [HttpPost]
    public Task<ApiResponse<SetupCurrencyDynamicApiResult>> SetupCurrencyDynamicApi(CancellationToken cancellationToken) =>
        SetupDynamicApiAsync(
            SampleDynamicApiConstants.CurrencyListApiId,
            SampleDynamicApiConstants.CurrencyListCode,
            """
            var items = await ctx.Db.Currencies
                .OrderBy(c => c.Code)
                .Select(c => new { c.Id, c.Name, c.Code })
                .ToListAsync(cancellationToken);
            return new ApiResponse { Value = items };
            """,
            cancellationToken);

    [HttpPost]
    public Task<ApiResponse<SetupCurrencyDynamicApiResult>> SetupCurrencyDynamicApiPaged(CancellationToken cancellationToken) =>
        SetupDynamicApiAsync(
            SampleDynamicApiConstants.CurrencyListPagedApiId,
            SampleDynamicApiConstants.CurrencyListPagedCode,
            """
            var pageStart = ctx.PageStart ?? 0;
            var pageSize = ctx.PageSize ?? 10;
            var query = ctx.Db.Currencies.OrderBy(c => c.Code);
            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip(pageStart)
                .Take(pageSize)
                .Select(c => new { c.Id, c.Name, c.Code })
                .ToListAsync(cancellationToken);
            return new PaginatedApiResponse(items, total, (pageStart / pageSize) + 1, pageSize);
            """,
            cancellationToken);

    private async Task<ApiResponse<SetupCurrencyDynamicApiResult>> SetupDynamicApiAsync(
        Guid apiId,
        string code,
        string sourceCode,
        CancellationToken cancellationToken)
    {
        var save = await _dynamicApiService.SaveDomainDynamicApiAsync(new SaveDomainDynamicApiCommand
        {
            Id = apiId,
            ApplicationId = SampleDynamicApiConstants.ApplicationId,
            MicroserviceId = SampleDynamicApiConstants.MicroserviceId,
            Code = code,
            NameTr = [new BaseEntityTranslationDto { LanguageId = Ids.Language.English.Id, Translation = "Currency list" }],
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
                CallUrl = $"/api/base/DynamicApi/Get/{code}"
            }
        };
    }
}

public class SetupCurrencyDynamicApiResult
{
    public Guid ApiId { get; set; }

    public required string Code { get; set; }

    public required string CallUrl { get; set; }
}