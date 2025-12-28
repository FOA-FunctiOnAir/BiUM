using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Core.Database;

public interface IBaseRepository
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
