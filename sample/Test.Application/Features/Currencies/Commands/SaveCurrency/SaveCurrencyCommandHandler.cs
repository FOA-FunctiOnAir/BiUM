using BiApp.Test.Application.Repositories;
using BiUM.Contract.Models.Api;
using BiUM.Specialized.Common.MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace BiApp.Test.Application.Features.Currencies.Commands.SaveCurrency;

public class SaveCurrencyCommandHandler : ICommandHandler<SaveCurrencyCommand>
{
    private readonly ICurrencyRepository _currencyRepository;

    public SaveCurrencyCommandHandler(ICurrencyRepository currencyRepository)
    {
        _currencyRepository = currencyRepository;
    }

    public async Task<ApiResponse> Handle(SaveCurrencyCommand command, CancellationToken cancellationToken)
    {
        var response = await _currencyRepository.SaveCurrency(command, cancellationToken);

        return response;
    }
}
