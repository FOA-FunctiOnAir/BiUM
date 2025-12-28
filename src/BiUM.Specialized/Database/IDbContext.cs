using BiUM.Infrastructure.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Database;

public interface IDbContext
{
    DatabaseFacade Database { get; }

    DbSet<DomainCrud> DomainCruds { get; }
    DbSet<DomainCrudColumn> DomainCrudColumns { get; }
    DbSet<DomainCrudTranslation> DomainCrudTranslations { get; }
    DbSet<DomainCrudVersion> DomainCrudVersions { get; }
    DbSet<DomainCrudVersionColumn> DomainCrudVersionColumns { get; }
    DbSet<DomainDynamicApi> DomainDynamicApis { get; }
    DbSet<DomainDynamicApiParameter> DomainDynamicApiParameters { get; }
    DbSet<DomainDynamicApiTranslation> DomainDynamicApiTranslations { get; }
    DbSet<DomainDynamicApiVersion> DomainDynamicApiVersions { get; }
    DbSet<DomainDynamicApiVersionParameter> DomainDynamicApiVersionParameters { get; }
    DbSet<DomainTranslation> DomainTranslations { get; }
    DbSet<DomainTranslationDetail> DomainTranslationDetails { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
