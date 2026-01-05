using BiUM.Specialized.Common.API;
using BiUM.Specialized.Common.MediatR;
using BiUM.Test2.Application.Repositories;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Test2.Application.Features.Accounts.Commands.DeleteAccount;

public class DeleteAccountCommandHandler : ICommandHandler<DeleteAccountCommand>
{
    private readonly IAccountRepository _currencyRepository;

    public DeleteAccountCommandHandler(IAccountRepository currencyRepository)
    {
        _currencyRepository = currencyRepository;
    }

    public async Task<ApiEmptyResponse> Handle(DeleteAccountCommand command, CancellationToken cancellationToken)
    {
        var response = await _currencyRepository.DeleteAccount(command.Id.Value, cancellationToken);

        return response;
    }
}
