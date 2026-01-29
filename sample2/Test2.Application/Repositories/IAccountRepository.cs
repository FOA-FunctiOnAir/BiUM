using BiApp.Test2.Application.Dtos;
using BiApp.Test2.Application.Features.Accounts.Commands.SaveAccount;
using BiApp.Test2.Application.Features.Accounts.Queries.GetFwAccountsForNames;
using BiApp.Test2.Application.Features.Accounts.Queries.GetFwAccountsForParameter;
using BiUM.Contract.Models.Api;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BiApp.Test2.Application.Repositories;

public interface IAccountRepository
{
    #region Queries

    Task<ApiResponse<string>> GetCurrency(Guid id, CancellationToken cancellationToken);

    Task<ApiResponse<AccountDto>> GetAccount(Guid id, CancellationToken cancellationToken);

    Task<ApiResponse<AccountDto>> GetAccountByCode(string code, CancellationToken cancellationToken);

    Task<PaginatedApiResponse<AccountsDto>> GetAccounts(Guid? id, string? name, string? code, int? pageStart, int? pageSize, CancellationToken cancellationToken);

    #endregion

    #region Command

    Task<ApiResponse> SaveAccount(SaveAccountCommand command, CancellationToken cancellationToken);

    Task<ApiResponse> DeleteAccount(Guid id, CancellationToken cancellationToken);

    #endregion

    #region Fw

    Task<PaginatedApiResponse<GetFwAccountsForParameterDto>> GetFwAccountsForParameter(string? q, int? pageStart, int? pageSize, CancellationToken cancellationToken);

    Task<ApiResponse<IList<GetFwAccountsForNamesDto>>> GetFwAccountsForNames(IReadOnlyList<Guid> ids, CancellationToken cancellationToken);

    #endregion

    #region Bolt

    Task<ApiResponse> UpdateBoltAccount(Guid id, CancellationToken cancellationToken);

    #endregion
}
