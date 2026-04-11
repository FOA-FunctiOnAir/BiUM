using BiApp.Test.Application.Repositories;
using BiUM.Contract.Models.Api;
using BiUM.Specialized.Common.MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace BiApp.Test.Application.Features.Currencies.Commands.UpdateCurrencyCode;

public class UpdateCurrencyCodeCommandHandler : ICommandHandler<UpdateCurrencyCodeCommand>
{
    private readonly ICurrencyRepository _currencyRepository;

    public UpdateCurrencyCodeCommandHandler(ICurrencyRepository currencyRepository)
    {
        _currencyRepository = currencyRepository;
    }

    public async Task<ApiResponse> Handle(UpdateCurrencyCodeCommand command, CancellationToken cancellationToken)
    {
        var response = await _currencyRepository.UpdateCurrencyCode(command, cancellationToken);

        return response;
    }
}