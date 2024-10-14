using BiUM.Specialized.Database;

namespace BiUM.Bolt.Database;

public interface IBaseBoltDbContextInitialiser : IDbContextInitialiser
{
    Task EqualizeAsync(CancellationToken cancellationToken = default);
}