using BiApp.Test.Domain.Entities;
using BiUM.Specialized.Database;
using Microsoft.EntityFrameworkCore;

namespace BiApp.Test.Infrastructure.Persistence;

public interface ITestDbContext : IDbContext, IBaseBoltDomainDbContext
{
    DbSet<Currency> Currencies { get; }
    DbSet<CurrencyTranslation> CurrencyTranslations { get; }
}
