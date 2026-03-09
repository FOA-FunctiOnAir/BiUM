using BiApp.Test2.Application.Dtos;
using BiApp.Test2.Application.Repositories;
using BiUM.Contract.Models.Api;
using BiUM.Specialized.Common;
using BiUM.Specialized.Common.MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiApp.Test2.Application.Features.Accounts.Queries.GetAccounts;

public class GetAccountsQueryHandler : ApplicationBase, IPaginatedQueryHandler<GetAccountsQuery, AccountsDto>
{
    private readonly IAccountRepository _currencyRepository;

    public GetAccountsQueryHandler(IServiceProvider serviceProvider, IAccountRepository currencyRepository) : base(serviceProvider)
    {
        _currencyRepository = currencyRepository;
    }

    public async Task<PaginatedApiResponse<AccountsDto>> Handle(GetAccountsQuery query, CancellationToken cancellationToken)
    {
        var repositoryResponse = await _currencyRepository.GetAccounts(
            query.Id,
            query.Name,
            query.Code,
            query.PageStart,
            query.PageSize,
            cancellationToken);

        return repositoryResponse;
    }
}