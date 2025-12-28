using BiUM.Contract;
using BiUM.Specialized.Services;
using BiUM.Test.Application.Repositories;
using BiUM.Test.Contract;
using Grpc.Core;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BiUM.Test.Infrastructure.GrpcServices;

public class TestGrpcService : TestApi.TestApiBase
{
    private readonly ICurrencyRepository _currencyRepository;
    private readonly ITranslationService _translationService;

    public TestGrpcService(
        ICurrencyRepository currencyRepository,
        ITranslationService translationService)
    {
        _currencyRepository = currencyRepository;
        _translationService = translationService;
    }

    public override async Task<GetCurrencyNamesResponse> GetCurrencyNames(GetCurrencyNamesRequest request, ServerCallContext context)
    {
        var response = new GetCurrencyNamesResponse() { Meta = new() { Success = true } };

        if (request.CurrencyIds.Count == 0)
        {
            await _translationService.AddMessage(response.Meta, "NoCurrency", context.CancellationToken);

            return response;
        }

        var currencyIds = request.CurrencyIds.ToGuidList();

        var currenciesResponse = await _currencyRepository.GetFwCurrenciesForNames(currencyIds, context.CancellationToken);

        if (!currenciesResponse.Success)
        {
            response.Meta.AddMessage(currenciesResponse.Messages);

            return response;
        }
        else if (currenciesResponse.Value is null)
        {
            await _translationService.AddMessage(response.Meta, "NoCurrency", context.CancellationToken);

            return response;
        }

        foreach (var currency in currenciesResponse.Value)
        {
            response.Items.Add(new GrpcIdNameMessage()
            {
                Id = currency.Id.ToString(),
                Name = currency.Name
            });
        }

        return response;
    }

    public override async Task<GetCurrencyResponse> GetCurrency(GetCurrencyRequest request, ServerCallContext context)
    {
        var response = new GetCurrencyResponse()
        {
            Meta = new() { Success = true }
        };

        if (string.IsNullOrWhiteSpace(request.CurrencyId))
        {
            await _translationService.AddMessage(response.Meta, "NoCurrencyCode", context.CancellationToken);

            return response;
        }

        var currencyId = request.CurrencyId.ToGuid();

        var currencyResponse = await _currencyRepository.GetCurrency(currencyId, context.CancellationToken);

        if (!currencyResponse.Success)
        {
            response.Meta.AddMessage(currencyResponse.Messages);

            return response;
        }

        if (currencyResponse.Value is null)
        {
            await _translationService.AddMessage(response.Meta, "CurrencyNotFound", context.CancellationToken);

            return response;
        }

        response.Currency = new GetCurrencyItem()
        {
            CurrencyId = currencyResponse.Value.Id.ToString(),
            CurrencyCode = currencyResponse.Value.Code,
            CurrencyName = currencyResponse.Value.Name,
            CurrencyType = currencyResponse.Value.Type.ToString(),
        };

        return response;
    }
}
