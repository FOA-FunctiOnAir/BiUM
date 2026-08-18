using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Database;
using BiUM.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BiUM.Tests.Transaction;

public sealed class TransactionalUnitOfWorkRunnerTests
{
    [Fact]
    public async Task RunAsync_commits_when_action_succeeds()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bium-uow-{Guid.NewGuid():N}.db");
        var correlation = new TestCorrelationContextProvider();
        var entityId = Guid.NewGuid();

        try
        {
            await using var sp = BiUMServiceFactory.BuildSqlite(correlation, path);
            await using var scope = sp.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<TestBiDbContext>();
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();

            var runner = new TransactionalUnitOfWorkRunner(db);

            await runner.RunAsync(async () =>
            {
                db.DomainCruds.Add(new DomainCrud
                {
                    Id = entityId,
                    Name = "test",
                    Code = "TEST",
                    TableName = "T_TEST"
                });
                await db.SaveChangesAsync();
            });

            db.Database.CurrentTransaction.Should().BeNull();

            await using var verifySp = BiUMServiceFactory.BuildSqlite(correlation, path);
            await using var verifyScope = verifySp.CreateAsyncScope();
            var verifyDb = verifyScope.ServiceProvider.GetRequiredService<TestBiDbContext>();
            var persisted = await verifyDb.DomainCruds.FirstOrDefaultAsync(c => c.Id == entityId);
            persisted.Should().NotBeNull();
        }
        finally
        {
            try
            {
                File.Delete(path);
            }
            catch
            {
            }
        }
    }

    [Fact]
    public async Task RunAsync_rolls_back_and_rethrows_when_action_throws()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bium-uow-{Guid.NewGuid():N}.db");
        var correlation = new TestCorrelationContextProvider();
        var entityId = Guid.NewGuid();

        try
        {
            await using var sp = BiUMServiceFactory.BuildSqlite(correlation, path);
            await using var scope = sp.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<TestBiDbContext>();
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();

            var runner = new TransactionalUnitOfWorkRunner(db);

            var act = async () => await runner.RunAsync(async () =>
            {
                db.DomainCruds.Add(new DomainCrud
                {
                    Id = entityId,
                    Name = "test",
                    Code = "TEST",
                    TableName = "T_TEST"
                });
                await db.SaveChangesAsync();

                throw new InvalidOperationException("boom");
            });

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");

            db.Database.CurrentTransaction.Should().BeNull();

            await using var verifySp = BiUMServiceFactory.BuildSqlite(correlation, path);
            await using var verifyScope = verifySp.CreateAsyncScope();
            var verifyDb = verifyScope.ServiceProvider.GetRequiredService<TestBiDbContext>();
            var persisted = await verifyDb.DomainCruds.FirstOrDefaultAsync(c => c.Id == entityId);
            persisted.Should().BeNull();
        }
        finally
        {
            try
            {
                File.Delete(path);
            }
            catch
            {
            }
        }
    }

    [Fact]
    public async Task RunAsync_without_dbcontext_still_invokes_action()
    {
        var runner = new TransactionalUnitOfWorkRunner();
        var invoked = false;

        await runner.RunAsync(() =>
        {
            invoked = true;
            return Task.CompletedTask;
        });

        invoked.Should().BeTrue();
    }

    [Fact]
    public async Task RunAsync_with_inmemory_provider_skips_transaction_but_invokes_action()
    {
        var correlation = new TestCorrelationContextProvider();

        await using var sp = BiUMServiceFactory.BuildInMemory(correlation, nameof(RunAsync_with_inmemory_provider_skips_transaction_but_invokes_action));
        await using var scope = sp.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TestBiDbContext>();

        var runner = new TransactionalUnitOfWorkRunner(db);
        var invoked = false;

        await runner.RunAsync(() =>
        {
            invoked = true;
            return Task.CompletedTask;
        });

        invoked.Should().BeTrue();
        db.Database.CurrentTransaction.Should().BeNull();
    }
}
