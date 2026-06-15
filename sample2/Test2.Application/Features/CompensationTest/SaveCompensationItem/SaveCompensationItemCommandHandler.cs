using BiApp.Test2.Application.Repositories;
using BiUM.Contract.Models.Api;
using BiUM.Specialized.Common.MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace BiApp.Test2.Application.Features.CompensationTest.SaveCompensationItem;

public class SaveCompensationItemCommandHandler : ICommandHandler<SaveCompensationItemCommand>
{
    private readonly ICompensationTestRepository _repository;

    public SaveCompensationItemCommandHandler(ICompensationTestRepository repository)
    {
        _repository = repository;
    }

    public async Task<ApiResponse> Handle(SaveCompensationItemCommand command, CancellationToken cancellationToken)
    {
        return await _repository.SaveCompensationItem(command, cancellationToken);
    }
}