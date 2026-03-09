using BiUM.Core.Database;
using BiUM.Specialized.Common;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Database;

public abstract partial class BaseRepository : InfrastructureBase, IBaseRepository
{
    protected IDbContext DbContext { get; }

    protected BaseRepository(IServiceProvider serviceProvider, IDbContext dbContext) : base(serviceProvider)
    {
        DbContext = dbContext;
    }

    protected virtual Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return DbContext.SaveChangesAsync(cancellationToken);
    }
}