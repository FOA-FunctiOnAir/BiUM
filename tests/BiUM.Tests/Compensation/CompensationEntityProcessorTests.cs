using BiUM.Core.Compensation;
using BiUM.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BiUM.Tests.Compensation;

public sealed class CompensationEntityProcessorTests
{
    [Fact]
    public async Task SaveChangesAsync_under_active_session_persists_compensatable_entity_via_sibling_context()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bium-comp-{Guid.NewGuid():N}.db");
        var sessionId = Guid.NewGuid();
        var correlation = new TestCorrelationContextProvider
        {
            Context = CorrelationTestHelper.CreateBpmnLike(Guid.NewGuid(), Guid.NewGuid(), compensationSessionId: sessionId)
        };
        var entityId = Guid.NewGuid();

        try
        {
            await using var sp = BiUMServiceFactory.BuildSqlite(correlation, path, withDbContextFactory: true);
            await using var scope = sp.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<TestBiDbContext>();
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();

            db.TestCompensatableEntities.Add(new TestCompensatableEntity { Id = entityId, Name = "test" });

            await db.SaveChangesAsync();

            await using var verifySp = BiUMServiceFactory.BuildSqlite(correlation, path);
            await using var verifyScope = verifySp.CreateAsyncScope();
            var verifyDb = verifyScope.ServiceProvider.GetRequiredService<TestBiDbContext>();
            var persisted = await verifyDb.TestCompensatableEntities
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => e.Id == entityId);

            persisted.Should().NotBeNull();
            persisted!.CStatus.Should().Be(CompensationStatusCodes.Insert);
            persisted.CompensationSessionId.Should().Be(sessionId);
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
    public async Task SaveChangesAsync_without_active_session_persists_directly_as_committed()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bium-comp-{Guid.NewGuid():N}.db");
        var correlation = new TestCorrelationContextProvider
        {
            Context = CorrelationTestHelper.CreateBpmnLike(Guid.NewGuid(), Guid.NewGuid())
        };
        var entityId = Guid.NewGuid();

        try
        {
            await using var sp = BiUMServiceFactory.BuildSqlite(correlation, path);
            await using var scope = sp.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<TestBiDbContext>();
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();

            db.TestCompensatableEntities.Add(new TestCompensatableEntity { Id = entityId, Name = "test" });

            await db.SaveChangesAsync();

            var persisted = await db.TestCompensatableEntities
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => e.Id == entityId);

            persisted.Should().NotBeNull();
            persisted!.CStatus.Should().Be(CompensationStatusCodes.Committed);
            persisted.CompensationSessionId.Should().BeNull();
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
}