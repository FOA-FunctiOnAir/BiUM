namespace BiUM.Core.Database;

public interface IBaseRepository
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}