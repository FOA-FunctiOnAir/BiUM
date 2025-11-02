using BiUM.Infrastructure.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace BiUM.Specialized.Database;

public interface IDbContext
{
    DatabaseFacade Database { get; }

    DbSet<DomainCrud> DomainCruds { get; }
    DbSet<DomainCrudColumn> DomainCrudColumns { get; }
    DbSet<DomainCrudTranslation> DomainCrudTranslations { get; }
    DbSet<DomainCrudVersion> DomainCrudVersions { get; }
    DbSet<DomainCrudVersionColumn> DomainCrudVersionColumns { get; }
    DbSet<DomainTranslation> DomainTranslations { get; }
    DbSet<DomainTranslationDetail> DomainTranslationDetails { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}