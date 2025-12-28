using System.Threading.Tasks;

namespace BiUM.Specialized.Database;

public interface IDbContextInitialiser
{
    Task InitialiseAsync();
    Task SeedAsync();
}
