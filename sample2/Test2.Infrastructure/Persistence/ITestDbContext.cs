using BiApp.Test2.Domain.Entities;
using BiUM.Specialized.Database;
using Microsoft.EntityFrameworkCore;

namespace BiApp.Test2.Infrastructure.Persistence;

public interface ITestDbContext : IDbContext, IBaseBoltDomainDbContext
{
    DbSet<Account> Accounts { get; }
    DbSet<AccountTranslation> AccountTranslations { get; }
}
