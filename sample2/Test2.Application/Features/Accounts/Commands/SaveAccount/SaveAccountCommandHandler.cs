using BiApp.Test2.Application.Repositories;
using BiUM.Contract.Models.Api;
using BiUM.Specialized.Common.MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace BiApp.Test2.Application.Features.Accounts.Commands.SaveAccount;

public class SaveAccountCommandHandler : ICommandHandler<SaveAccountCommand>
{
    private readonly IAccountRepository _currencyRepository;

    public SaveAccountCommandHandler(IAccountRepository currencyRepository)
    {
        _currencyRepository = currencyRepository;
    }

    public async Task<ApiResponse> Handle(SaveAccountCommand command, CancellationToken cancellationToken)
    {
        var response = await _currencyRepository.SaveAccount(command, cancellationToken);

        return response;
    }
}