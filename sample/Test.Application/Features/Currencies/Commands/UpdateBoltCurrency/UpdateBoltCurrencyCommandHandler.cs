using BiUM.Specialized.Common.API;
using BiUM.Specialized.Common.MediatR;
using BiUM.Test.Application.Repositories;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Test.Application.Features.Currencies.Commands.UpdateBoltCurrency;

public class UpdateBoltCurrencyCommandHandler : ICommandHandler<UpdateBoltCurrencyCommand>
{
    private readonly ICurrencyRepository _currencyRepository;

    public UpdateBoltCurrencyCommandHandler(ICurrencyRepository currencyRepository)
    {
        _currencyRepository = currencyRepository;
    }

    public async Task<ApiEmptyResponse> Handle(UpdateBoltCurrencyCommand command, CancellationToken cancellationToken)
    {
        var response = await _currencyRepository.UpdateBoltCurrency(command.Id!.Value, cancellationToken);

        return response;
    }
}