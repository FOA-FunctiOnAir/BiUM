using BiUM.Specialized.Database;
using BiUM.Test2.Application.Repositories;
using BiUM.Test2.Contract.Services;
using BiUM.Test2.Infrastructure.Persistence;
using System;

namespace BiUM.Test2.Infrastructure.Repositories;

public partial class AccountRepository : BaseRepository, IAccountRepository
{
    private readonly ITestDbContext _context;
    private readonly IBoltDbContext _boltContext;
    private readonly ITestRpcService _testRpcService;

    public AccountRepository(
        IServiceProvider serviceProvider,
        ITestDbContext context,
        IBoltDbContext boltContext,
        ITestRpcService testRpcService) : base(serviceProvider, context)
    {
        _context = context;
        _boltContext = boltContext;
        _testRpcService = testRpcService;
    }
}
