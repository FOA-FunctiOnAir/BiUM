using BiUM.Test.Application.Repositories;
using BiUM.Specialized.Common.API;
using BiUM.Specialized.Common.MediatR;

namespace BiUM.Test.Application.Features.Currencies.Commands.SaveCurrency;

public class SaveCurrencyCommandHandler : ICommandHandler<SaveCurrencyCommand>
{
    private readonly ICurrencyRepository _currencyRepository;

    public SaveCurrencyCommandHandler(ICurrencyRepository currencyRepository)
    {
        _currencyRepository = currencyRepository;
    }

    public async Task<ApiEmptyResponse> Handle(SaveCurrencyCommand command, CancellationToken cancellationToken)
    {
        var response = await _currencyRepository.SaveCurrency(command, cancellationToken);

        return response;
    }
}