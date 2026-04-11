using BiUM.Core.Compensation;
using BiUM.Specialized.Services.Compensation;
using BiUM.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BiUM.Tests.Compensation;

public sealed class CompensationServiceTests
{
    [Fact]
    public async Task CommitSessionAsync_no_pending_snapshots_completes()
    {
        var correlation = new TestCorrelationContextProvider
        {
            Context = CorrelationTestHelper.CreateBpmnLike(Guid.NewGuid(), Guid.NewGuid())
        };

        await using var sp = BiUMServiceFactory.BuildInMemory(correlation, Guid.NewGuid().ToString("N"));
        using var scope = sp.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<ICompensationService>();

        var act = async () => await svc.CommitSessionAsync(Guid.NewGuid(), CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RollbackSessionAsync_no_pending_snapshots_completes()
    {
        var correlation = new TestCorrelationContextProvider
        {
            Context = CorrelationTestHelper.CreateBpmnLike(Guid.NewGuid(), Guid.NewGuid())
        };

        await using var sp = BiUMServiceFactory.BuildInMemory(correlation, Guid.NewGuid().ToString("N"));
        using var scope = sp.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<ICompensationService>();

        var act = async () => await svc.RollbackSessionAsync(Guid.NewGuid(), CancellationToken.None);
        await act.Should().NotThrowAsync();
    }
}
