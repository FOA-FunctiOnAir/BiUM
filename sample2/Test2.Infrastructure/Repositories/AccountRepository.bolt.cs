using BiUM.Contract.Models.Api;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BiApp.Test2.Infrastructure.Repositories;

public partial class AccountRepository
{
    public async Task<ApiResponse> UpdateBoltAccount(Guid id, CancellationToken cancellationToken)
    {
        var returnObject = new ApiResponse();

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
