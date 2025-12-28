using BiUM.Specialized.Database;
using BiUM.Test.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BiUM.Test.Infrastructure.Persistence;

public interface ITestDbContext : IDbContext, IBaseBoltDomainDbContext
{
    DbSet<Currency> Currencies { get; }
    DbSet<CurrencyTranslation> CurrencyTranslations { get; }
}
