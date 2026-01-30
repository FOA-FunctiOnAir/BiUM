using BiApp.Test.Application.Repositories;
using BiUM.Contract.Models.Api;
using BiUM.Specialized.Common.MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiApp.Test.Application.Features.Currencies.Commands.DeleteCurrency;

public class DeleteCurrencyCommandHandler : ICommandHandler<DeleteCurrencyCommand>
{
    private readonly ICurrencyRepository _currencyRepository;

    public DeleteCurrencyCommandHandler(ICurrencyRepository currencyRepository)
    {
        _currencyRepository = currencyRepository;
    }

    public async Task<ApiResponse> Handle(DeleteCurrencyCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command.Id);

        var response = await _currencyRepository.DeleteCurrency(command.Id.Value, cancellationToken);

        return response;
    }
}