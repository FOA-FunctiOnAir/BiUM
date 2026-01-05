using BiUM.Specialized.Common.API;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Test2.Infrastructure.Repositories;

public partial class AccountRepository
{
    public async Task<ApiEmptyResponse> UpdateBoltAccount(Guid id, CancellationToken cancellationToken)
    {
        var returnObject = new ApiEmptyResponse();

        var account = await _context.Accounts.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

        if (account != null)
        {
            var currencyTranslations = await _context.AccountTranslations.AsNoTracking()
                .Where(f => f.RecordId == account!.Id)
                .ToListAsync(cancellationToken);

            await _boltContext.AddOrUpdate(1, nameof(_boltContext.Accounts), account, cancellationToken);
            await _boltContext.AddOrUpdate(2, nameof(_boltContext.AccountTranslations), currencyTranslations, cancellationToken);

            await _boltContext.SaveChangesAsync(cancellationToken);
        }

        return returnObject;
    }
}
