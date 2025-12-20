using BiUM.Specialized.Common.API;
using BiUM.Specialized.Database;
using BiUM.Test.Application.Features.Currencies.Commands.SaveCurrency;
using BiUM.Test.Application.Repositories;
using BiUM.Test.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Test.Infrastructure.Repositories;

public partial class CurrencyRepository
{
    public async Task<ApiEmptyResponse> SaveCurrency(SaveCurrencyCommand command, CancellationToken cancellationToken)
    {
        var response = new ApiEmptyResponse();

        var currency = await _context.Currencies.FirstOrDefaultAsync(t => t.Id == command.Id, cancellationToken);

        if (currency is null)
        {
            currency = new Currency()
            {
                Name = command.NameTr.ToTranslationString(),
                Code = command.Code,
            };

            _context.Currencies.Add(currency);
        }
        else
        {
            currency.Name = command.NameTr.ToTranslationString();
            currency.Code = command.Code;

            _context.Currencies.Update(currency);
        }

        await SaveTranslations(_context.CurrencyTranslations, currency!.Id, nameof(currency.Name), command.NameTr, cancellationToken);

        await SaveChangesAsync(cancellationToken);

        return response;
    }

    public async Task<ApiEmptyResponse> DeleteCurrency(Guid id, CancellationToken cancellationToken)
    {
        var response = new ApiEmptyResponse();

        var entity = await _context.Currencies.FindAsync([id], cancellationToken);

        if (entity is null)
        {
            await AddMessage(response, "CurrencyNotFound", cancellationToken);

            return response;
        }

        _context.Currencies.Remove(entity);

        await SaveChangesAsync(cancellationToken);

        return response;
    }
}