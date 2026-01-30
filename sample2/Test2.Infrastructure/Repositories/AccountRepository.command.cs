using BiApp.Test2.Application.Features.Accounts.Commands.SaveAccount;
using BiApp.Test2.Domain.Entities;
using BiUM.Contract.Models.Api;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BiApp.Test2.Infrastructure.Repositories;

public partial class AccountRepository
{
    public async Task<ApiResponse> SaveAccount(SaveAccountCommand command, CancellationToken cancellationToken)
    {
        var response = new ApiResponse();

        var account = await _context.Accounts.FirstOrDefaultAsync(t => t.Id == command.Id, cancellationToken);

        if (account is null)
        {
            account = new Account
            {
                Id = command.Id!.Value,
                Name = command.NameTr.ToTranslationString(),
                Code = command.Code,
            };

            _ = _context.Accounts.Add(account);
        }
        else
        {
            account.Name = command.NameTr.ToTranslationString();
            account.Code = command.Code;

            _ = _context.Accounts.Update(account);
        }

        await SaveTranslations(_context.AccountTranslations, account.Id, nameof(account.Name), command.NameTr, cancellationToken);

        _ = await SaveChangesAsync(cancellationToken);

        return response;
    }

    public async Task<ApiResponse> DeleteAccount(Guid id, CancellationToken cancellationToken)
    {
        var response = new ApiResponse();

        var entity = await _context.Accounts.FindAsync([id], cancellationToken);

        if (entity is null)
        {
            _ = await AddMessage(response, "AccountNotFound", cancellationToken);

            return response;
        }

        _ = _context.Accounts.Remove(entity);

        _ = await SaveChangesAsync(cancellationToken);

        return response;
    }
}