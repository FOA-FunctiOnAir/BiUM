using BiUM.Contract.Models.Api;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Test.Infrastructure.Repositories;

public partial class CurrencyRepository
{
    public async Task<ApiResponse> UpdateBoltCurrency(Guid id, CancellationToken cancellationToken)
    {
        var returnObject = new ApiResponse();

        var currency = await _context.Currencies.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

        if (currency != null)
        {
            var currencyTranslations = await _context.CurrencyTranslations.AsNoTracking()
                .Where(f => f.RecordId == currency!.Id)
                .ToListAsync(cancellationToken);

            await _boltContext.AddOrUpdate(1, nameof(_boltContext.Currencies), currency, cancellationToken);
            await _boltContext.AddOrUpdate(2, nameof(_boltContext.CurrencyTranslations), currencyTranslations, cancellationToken);

            await _boltContext.SaveChangesAsync(cancellationToken);
        }

        return returnObject;
    }
}
