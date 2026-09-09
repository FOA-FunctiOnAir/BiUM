using BiUM.Specialized.Middlewares;
using BiUM.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Security.Claims;
using Xunit;

namespace BiUM.Tests.Transaction;

public sealed class HubTransactionFilterTests
{
    [Fact]
    public async Task Sqlite_commits_transaction_when_next_succeeds()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bium-hubtx-{Guid.NewGuid():N}.db");
        var correlation = new TestCorrelationContextProvider();

        try
        {
            await using var sp = BiUMServiceFactory.BuildSqlite(correlation, path);
            await using var scope = sp.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<TestBiDbContext>();
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();

            var filter = new HubTransactionFilter();
            var invocationContext = CreateInvocationContext(scope.ServiceProvider);

            var result = await filter.InvokeMethodAsync(invocationContext, _ => ValueTask.FromResult<object?>("ok"));

            result.Should().Be("ok");
            db.Database.CurrentTransaction.Should().BeNull();
        }
        finally
        {
            try { File.Delete(path); } catch { }
        }
    }

    [Fact]
    public async Task Sqlite_rolls_back_when_next_throws()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bium-hubtx-{Guid.NewGuid():N}.db");
        var correlation = new TestCorrelationContextProvider();

        try
        {
            await using var sp = BiUMServiceFactory.BuildSqlite(correlation, path);
            await using var scope = sp.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<TestBiDbContext>();
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();

            var filter = new HubTransactionFilter();
            var invocationContext = CreateInvocationContext(scope.ServiceProvider);

            var act = async () => await filter.InvokeMethodAsync(
                invocationContext,
                _ => throw new InvalidOperationException("boom"));

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");

            db.Database.CurrentTransaction.Should().BeNull();
        }
        finally
        {
            try { File.Delete(path); } catch { }
        }
    }

    [Fact]
    public async Task No_db_context_available_calls_next_directly()
    {
        var services = new ServiceCollection();
        await using var sp = services.BuildServiceProvider();
        await using var scope = sp.CreateAsyncScope();

        var filter = new HubTransactionFilter();
        var invocationContext = CreateInvocationContext(scope.ServiceProvider);

        var called = false;

        var result = await filter.InvokeMethodAsync(invocationContext, _ =>
        {
            called = true;
            return ValueTask.FromResult<object?>(null);
        });

        called.Should().BeTrue();
        result.Should().BeNull();
    }

    private static HubInvocationContext CreateInvocationContext(IServiceProvider serviceProvider) =>
        new(
            new TestHubCallerContext(),
            serviceProvider,
            new TestHub(),
            TestMethodInfo,
            Array.Empty<object?>());

    private static readonly MethodInfo TestMethodInfo =
        typeof(TestHub).GetMethod(nameof(TestHub.TestMethod))!;

    private sealed class TestHub : Hub
    {
        public void TestMethod()
        {
        }
    }

    private sealed class TestHubCallerContext : HubCallerContext
    {
        public override string ConnectionId { get; } = Guid.NewGuid().ToString("N");
        public override string? UserIdentifier => null;
        public override ClaimsPrincipal? User => null;
        public override IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();
        public override IFeatureCollection Features { get; } = new FeatureCollection();
        public override CancellationToken ConnectionAborted => CancellationToken.None;

        public override void Abort()
        {
        }
    }
}
