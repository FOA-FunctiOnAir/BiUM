using BiUM.Specialized.Common.API;
using BiUM.Specialized.Common.MediatR;
using BiUM.Test2.Application.Repositories;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Test2.Application.Features.Accounts.Commands.SaveAccount;

public class SaveAccountCommandHandler : ICommandHandler<SaveAccountCommand>
{
    private readonly IAccountRepository _currencyRepository;

    public SaveAccountCommandHandler(IAccountRepository currencyRepository)
    {
        _currencyRepository = currencyRepository;
    }

    public async Task<ApiEmptyResponse> Handle(SaveAccountCommand command, CancellationToken cancellationToken)
    {
        var response = await _currencyRepository.SaveAccount(command, cancellationToken);

        return response;
    }
}
