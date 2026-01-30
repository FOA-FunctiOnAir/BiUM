using BiUM.Contract.Models.Api;
using BiUM.Specialized.Common.Translation;
using BiUM.Specialized.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Common.API;

[ApiController]
[BiUMBaseRoute]
public class DomainTranslationController : ApiControllerBase
{
    private readonly ITranslationService _translationService;

    public DomainTranslationController(ITranslationService translationService)
    {
        _translationService = translationService;
    }

    [HttpPost]
    public async Task<ApiResponse> SaveDomainTranslationAsync([FromBody] SaveDomainTranslationCommand command, CancellationToken cancellationToken)
    {
        var response = await _translationService.SaveDomainTranslationAsync(command, cancellationToken);

        return response;
    }

    [HttpDelete]
    public async Task<ApiResponse> DeleteDomainTranslationAsync([FromBody] DeleteDomainTranslationCommand command, CancellationToken cancellationToken)
    {
        var response = await _translationService.DeleteDomainTranslationAsync(command.Id!.Value, cancellationToken);

        return response;
    }

    [HttpGet]
    public async Task<ApiResponse<DomainTranslationDto>> GetDomainTranslationAsync(string id, CancellationToken cancellationToken)
    {
        Guid.TryParse(id, out var guidId);

        var response = await _translationService.GetDomainTranslationAsync(guidId, cancellationToken);

        return response;
    }

    [HttpGet]
    public async Task<PaginatedApiResponse<DomainTranslationsDto>> GetDomainTranslationsAsync([FromQuery] GetDomainTranslationsQuery query, CancellationToken cancellationToken)
    {

        var response = await _translationService.GetDomainTranslationsAsync(query.Code, query.Q, query.PageStart, query.PageSize, cancellationToken);

        return response;
    }
}