using BiUM.Contract.Models.Api;
using BiUM.Specialized.Common.MediatR;
using BiUM.Test.Application.Repositories;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Test.Application.Features.Currencies.Commands.DeleteCurrency;

public class DeleteCurrencyCommandHandler : ICommandHandler<DeleteCurrencyCommand>
{
    private readonly ICurrencyRepository _currencyRepository;

    public DeleteCurrencyCommandHandler(ICurrencyRepository currencyRepository)
    {
        _currencyRepository = currencyRepository;
    }

    public async Task<ApiResponse> Handle(DeleteCurrencyCommand command, CancellationToken cancellationToken)
    {
        var response = await _currencyRepository.DeleteCurrency(command.Id.Value, cancellationToken);

        return response;
    }
}
