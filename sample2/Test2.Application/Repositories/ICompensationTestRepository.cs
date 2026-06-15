using BiApp.Test2.Application.Features.CompensationTest.SaveCompensationItem;
using BiUM.Contract.Models.Api;
using System.Threading;
using System.Threading.Tasks;

namespace BiApp.Test2.Application.Repositories;

public interface ICompensationTestRepository
{
    Task<ApiResponse> SaveCompensationItem(SaveCompensationItemCommand command, CancellationToken cancellationToken);
}