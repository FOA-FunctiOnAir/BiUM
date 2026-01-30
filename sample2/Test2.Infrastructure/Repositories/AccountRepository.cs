using BiApp.Test2.Application.Repositories;
using BiApp.Test2.Contract.Services.Rpc;
using BiApp.Test2.Infrastructure.Persistence;
using BiUM.Specialized.Database;
using System;

namespace BiApp.Test2.Infrastructure.Repositories;

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