using BiApp.Test.Application.Features.Currencies.Commands.SaveCurrency;
using BiUM.Contract.Models.Api;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BiApp.Test.Infrastructure.Repositories;

public partial class CurrencyRepository
{
    public async Task<ApiResponse> SaveCurrency(SaveCurrencyCommand command, CancellationToken cancellationToken)
    {
        var response = new ApiResponse();

        var currency = await _context.Currencies.FirstOrDefaultAsync(t => t.Id == command.Id, cancellationToken);

        if (currency is null)
        {
            currency = new Currency
            {
                Id = command.Id!.Value,
                Name = command.NameTr.ToTranslationString(),
                Code = command.Code,
            };

            _ = _context.Currencies.Add(currency);
        }
        else
        {
            currency.Name = command.NameTr.ToTranslationString();
            currency.Code = command.Code;

            _ = _context.Currencies.Update(currency);
        }

        await SaveTranslations(_context.CurrencyTranslations, currency!.Id, nameof(currency.Name), command.NameTr, cancellationToken);

        _ = await SaveChangesAsync(cancellationToken);

        return response;
    }

    public async Task<ApiResponse> DeleteCurrency(Guid id, CancellationToken cancellationToken)
    {
        var response = new ApiResponse();

        var entity = await _context.Currencies.FindAsync([id], cancellationToken);

        if (entity is null)
        {
            _ = await AddMessage(response, "CurrencyNotFound", cancellationToken);

            return response;
        }

        _ = _context.Currencies.Remove(entity);

        _ = await SaveChangesAsync(cancellationToken);

        return response;
    }
}
