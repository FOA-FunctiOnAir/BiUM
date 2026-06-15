using BiApp.Test2.Application.Features.CompensationTest.SaveCompensationItem;
using BiApp.Test2.Application.Repositories;
using BiApp.Test2.Domain.Entities;
using BiApp.Test2.Infrastructure.Persistence;
using BiUM.Contract.Models.Api;
using BiUM.Specialized.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiApp.Test2.Infrastructure.Repositories;

public class CompensationTestRepository : BaseRepository, ICompensationTestRepository
{
    private readonly ITestDbContext _context;

    public CompensationTestRepository(IServiceProvider serviceProvider, ITestDbContext context)
        : base(serviceProvider, context)
    {
        _context = context;
    }

    public async Task<ApiResponse> SaveCompensationItem(SaveCompensationItemCommand command, CancellationToken cancellationToken)
    {
        var item = command.Id.HasValue
            ? await _context.CompensationItems.FirstOrDefaultAsync(x => x.Id == command.Id.Value, cancellationToken)
            : null;

        if (item is null)
        {
            item = new CompensationItem
            {
                Id = command.Id ?? Guid.NewGuid(),
                Name = command.Name,
            };

            _ = _context.CompensationItems.Add(item);
        }
        else
        {
            item.Name = command.Name;
            _ = _context.CompensationItems.Update(item);
        }

        _ = await SaveChangesAsync(cancellationToken);

        return new ApiResponse();
    }
}