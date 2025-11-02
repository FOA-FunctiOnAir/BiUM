using BiUM.Test.Domain.Entities;
using BiUM.Specialized.Database;
using Microsoft.EntityFrameworkCore;

namespace BiUM.Test.Infrastructure.Persistence;

public interface ITestDbContext : IDbContext, IBaseBoltDomainDbContext
{
    DbSet<Currency> Currencies { get; }
    DbSet<CurrencyTranslation> CurrencyTranslations { get; }
}