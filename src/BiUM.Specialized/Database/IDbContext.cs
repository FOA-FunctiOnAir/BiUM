namespace BiUM.Specialized.Database;

public interface IDbContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}