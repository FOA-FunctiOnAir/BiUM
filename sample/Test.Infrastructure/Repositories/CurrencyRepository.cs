using BiUM.Specialized.Database;
using BiUM.Test.Application.Repositories;
using BiUM.Test.Infrastructure.Persistence;
using System;

namespace BiUM.Test.Infrastructure.Repositories;

public partial class CurrencyRepository : BaseRepository, ICurrencyRepository
{
    private readonly ITestDbContext _context;
    private readonly IBoltDbContext _boltContext;

    public CurrencyRepository(
        IServiceProvider serviceProvider,
        ITestDbContext context,
        IBoltDbContext boltContext) : base(serviceProvider, context)
    {
        _context = context;
        _boltContext = boltContext;
    }
}
