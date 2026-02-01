using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Database;

public interface IDbContextInitialiser
{
    Task InitialiseAsync(CancellationToken cancellationToken = default);
    Task SeedAsync(CancellationToken cancellationToken = default);
}