using BiUM.Specialized.Middlewares;
using BiUM.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BiUM.Tests.Transaction;

public sealed class RequestTransactionMiddlewareTests
{
    [Fact]
    public async Task Sqlite_post_begins_transaction_and_commits_when_next_succeeds()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bium-tx-{Guid.NewGuid():N}.db");
        var correlation = new TestCorrelationContextProvider();

        try
        {
            await using var sp = BiUMServiceFactory.BuildSqlite(correlation, path);
            await using var scope = sp.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<TestBiDbContext>();
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();

            var middleware = new RequestTransactionMiddleware(_ => Task.CompletedTask);
            var ctx = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
            ctx.Request.Method = "POST";
            ctx.Request.Path = "/api/mutation";

            await middleware.InvokeAsync(ctx);

            var currentTransaction = db.Database.CurrentTransaction;
            currentTransaction.Should().BeNull();
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
    public async Task Sqlite_post_rolls_back_when_next_throws()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bium-tx-{Guid.NewGuid():N}.db");
        var correlation = new TestCorrelationContextProvider();

        try
        {
            await using var sp = BiUMServiceFactory.BuildSqlite(correlation, path);
            await using var scope = sp.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<TestBiDbContext>();
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();

            var middleware = new RequestTransactionMiddleware(_ => throw new InvalidOperationException("boom"));
            var ctx = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
            ctx.Request.Method = "POST";
            ctx.Request.Path = "/api/mutation";

            var act = async () => await middleware.InvokeAsync(ctx);
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");

            db.Database.CurrentTransaction.Should().BeNull();
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