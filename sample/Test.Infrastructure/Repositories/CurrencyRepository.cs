using BiApp.Test.Application.Repositories;
using BiApp.Test.Infrastructure.Persistence;
using BiUM.Specialized.Database;
using System;

namespace BiApp.Test.Infrastructure.Repositories;

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
