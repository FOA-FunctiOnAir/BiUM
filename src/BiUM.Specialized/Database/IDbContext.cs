using BiUM.Infrastructure.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace BiUM.Specialized.Database;

public interface IDbContext
{
    DbSet<DomainTranslation> DomainTranslations { get; }
    DbSet<DomainTranslationDetail> DomainTranslationDetails { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}