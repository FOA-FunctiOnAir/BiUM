using BiApp.Test2.Application.Features.Accounts.Queries.GetFwAccountsForNames;
using BiApp.Test2.Application.Features.Accounts.Queries.GetFwAccountsForParameter;
using BiApp.Test2.Domain.Entities;
using BiUM.Contract.Models.Api;
using BiUM.Specialized.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BiApp.Test2.Infrastructure.Repositories;

public partial class AccountRepository
{
    public async Task<PaginatedApiResponse<GetFwAccountsForParameterDto>> GetFwAccountsForParameter(
        string? q,
        int? pageStart,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        var currencies = await _context.Accounts
            .Include(c => c.AccountTranslations.Where(ct => ct.LanguageId == CorrelationContext.LanguageId))
            .Where(c =>
                string.IsNullOrEmpty(q) ||
                string.IsNullOrEmpty(c.Name) || c.Name.Contains(q) ||
                string.IsNullOrEmpty(c.Code) || c.Code.Contains(q)
            )
            .ToPaginatedListAsync<Account, GetFwAccountsForParameterDto>(Mapper, pageStart, pageSize, cancellationToken);

        return currencies;
    }

    public async Task<ApiResponse<IList<GetFwAccountsForNamesDto>>> GetFwAccountsForNames(IReadOnlyList<Guid>? ids, CancellationToken cancellationToken)
    {
        var returnObject = new ApiResponse<IList<GetFwAccountsForNamesDto>>();

        var currencies = await _context.Accounts
            .Include(c => c.AccountTranslations.Where(ct => ct.LanguageId == CorrelationContext.LanguageId))
            .Where(m => ids != null && ids.Contains(m.Id))
            .ToIListAsync<Account, GetFwAccountsForNamesDto>(Mapper, cancellationToken);

        returnObject.Value = currencies;

        return returnObject;
    }
}
