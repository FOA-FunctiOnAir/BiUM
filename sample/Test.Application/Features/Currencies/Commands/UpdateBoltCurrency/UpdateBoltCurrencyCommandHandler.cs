using BiApp.Test.Application.Repositories;
using BiUM.Contract.Models.Api;
using BiUM.Specialized.Common.MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiApp.Test.Application.Features.Currencies.Commands.UpdateBoltCurrency;

public class UpdateBoltCurrencyCommandHandler : ICommandHandler<UpdateBoltCurrencyCommand>
{
    private readonly ICurrencyRepository _currencyRepository;

    public UpdateBoltCurrencyCommandHandler(ICurrencyRepository currencyRepository)
    {
        _currencyRepository = currencyRepository;
    }

    public async Task<ApiResponse> Handle(UpdateBoltCurrencyCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command.Id);

        var response = await _currencyRepository.UpdateBoltCurrency(command.Id.Value, cancellationToken);

        return response;
    }
}