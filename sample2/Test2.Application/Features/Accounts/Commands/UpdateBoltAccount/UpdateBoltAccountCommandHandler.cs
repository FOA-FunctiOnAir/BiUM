using BiApp.Test2.Application.Repositories;
using BiUM.Contract.Models.Api;
using BiUM.Specialized.Common.MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace BiApp.Test2.Application.Features.Accounts.Commands.UpdateBoltAccount;

public class UpdateBoltAccountCommandHandler : ICommandHandler<UpdateBoltAccountCommand>
{
    private readonly IAccountRepository _currencyRepository;

    public UpdateBoltAccountCommandHandler(IAccountRepository currencyRepository)
    {
        _currencyRepository = currencyRepository;
    }

    public async Task<ApiResponse> Handle(UpdateBoltAccountCommand command, CancellationToken cancellationToken)
    {
        var response = await _currencyRepository.UpdateBoltAccount(command.Id!.Value, cancellationToken);

        return response;
    }
}