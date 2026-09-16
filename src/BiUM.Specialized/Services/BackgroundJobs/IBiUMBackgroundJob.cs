using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Services.BackgroundJobs;

public interface IBiUMBackgroundJob
{
    Task ExecuteAsync(CancellationToken cancellationToken);
}