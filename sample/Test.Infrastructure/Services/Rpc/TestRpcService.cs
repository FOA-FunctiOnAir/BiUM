using BiApp.Test.Application.Repositories;
using BiApp.Test.Contract.Models.Rpc;
using BiApp.Test.Contract.Services.Rpc;
using BiUM.Specialized.Services;
using MagicOnion;
using MagicOnion.Server;
using System;

namespace BiApp.Test.Infrastructure.Services.Rpc;

public sealed class TestRpcService : ServiceBase<ITestRpcService>, ITestRpcService
{
    private readonly ICurrencyRepository _currencyRepository;
    private readonly ITranslationService _translationService;

    public TestRpcService(
        ICurrencyRepository currencyRepository,
        ITranslationService translationService)
    {
        _currencyRepository = currencyRepository;
        _translationService = translationService;
    }

    public async UnaryResult<GetCurrencyResponse> GetCurrency(GetCurrencyRequest request)
    {
        var cancellationToken = Context.CallContext.CancellationToken;

        var response = new GetCurrencyResponse();

        if (string.IsNullOrWhiteSpace(request.CurrencyId))
        {
            await _translationService.AddMessage(response, "NoCurrencyCode", cancellationToken);

            return response;
        }

        var currencyId = request.CurrencyId.ToGuid();

        var currencyResponse = await _currencyRepository.GetCurrency(currencyId, cancellationToken);

        if (!currencyResponse.Success)
        {
            response.AddMessage(currencyResponse.Messages);

            return response;
        }

        if (currencyResponse.Value is null)
        {
            await _translationService.AddMessage(response, "CurrencyNotFound", cancellationToken);

            return response;
        }

        response.Value = new()
        {
            CurrencyId = currencyResponse.Value.Id.ToString(),
            CurrencyCode = currencyResponse.Value.Code,
            CurrencyType = currencyResponse.Value.Type.ToString(),
            CurrencyName = currencyResponse.Value.Name
        };

        return response;
    }
}