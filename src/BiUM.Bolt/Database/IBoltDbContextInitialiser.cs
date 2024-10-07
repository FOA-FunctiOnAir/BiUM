using BiUM.Specialized.Database;

namespace BiUM.Bolt.Database;

public interface IBoltDbContextInitialiser : IDbContextInitialiser
{
    Task EqualizeAsync(CancellationToken cancellationToken = default);
}