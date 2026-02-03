using BiUM.Specialized.Database;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Bolt.Database;

public interface IBoltDbContextInitialiser : IDbContextInitialiser
{
    Task EqualizeAsync(CancellationToken cancellationToken = default);
}