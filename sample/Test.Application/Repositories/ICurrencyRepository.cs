using BiUM.Contract.Models.Api;
using BiUM.Test.Application.Dtos;
using BiUM.Test.Application.Features.Currencies.Commands.SaveCurrency;
using BiUM.Test.Application.Features.Currencies.Queries.GetFwCurrenciesForNames;
using BiUM.Test.Application.Features.Currencies.Queries.GetFwCurrenciesForParameter;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Test.Application.Repositories;

public interface ICurrencyRepository
{
    #region Queries

    Task<ApiResponse<CurrencyDto>> GetCurrency(Guid id, CancellationToken cancellationToken);

    Task<ApiResponse<CurrencyDto>> GetCurrencyByCode(string code, CancellationToken cancellationToken);

    Task<PaginatedApiResponse<CurrenciesDto>> GetCurrencies(Guid? id, string? name, string? code, int? pageStart, int? pageSize, CancellationToken cancellationToken);

    #endregion

    #region Command

    Task<ApiResponse> SaveCurrency(SaveCurrencyCommand command, CancellationToken cancellationToken);

    Task<ApiResponse> DeleteCurrency(Guid id, CancellationToken cancellationToken);

    #endregion

    #region Fw

    Task<PaginatedApiResponse<GetFwCurrenciesForParameterDto>> GetFwCurrenciesForParameter(string? q, int? pageStart, int? pageSize, CancellationToken cancellationToken);

    Task<ApiResponse<IList<GetFwCurrenciesForNamesDto>>> GetFwCurrenciesForNames(IReadOnlyList<Guid>? ids, CancellationToken cancellationToken);

    #endregion

    #region Bolt

    Task<ApiResponse> UpdateBoltCurrency(Guid id, CancellationToken cancellationToken);

    #endregion
}
