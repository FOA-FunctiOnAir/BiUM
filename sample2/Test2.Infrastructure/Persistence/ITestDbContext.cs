using BiUM.Specialized.Database;
using BiUM.Test2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BiUM.Test2.Infrastructure.Persistence;

public interface ITestDbContext : IDbContext, IBaseBoltDomainDbContext
{
    DbSet<Account> Accounts { get; }
    DbSet<AccountTranslation> AccountTranslations { get; }
}
