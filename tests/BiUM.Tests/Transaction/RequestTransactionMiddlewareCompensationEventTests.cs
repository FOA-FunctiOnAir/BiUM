using BiUM.Core.Compensation;
using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Common.API;
using BiUM.Specialized.Middlewares;
using BiUM.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BiUM.Tests.Transaction;

public sealed class RequestTransactionMiddlewareCompensationEventTests
{
    [Fact]
    public async Task Success_path_publishes_finalized_event_after_transaction_commits()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bium-finalize-{Guid.NewGuid():N}.db");
        var correlation = new TestCorrelationContextProvider();
        var sessionId = Guid.NewGuid();
        var publisher = new RecordingCompensationSessionFinalizedPublisher();

        try
        {
            await using var sp = BiUMServiceFactory.BuildSqlite(correlation, path, services =>
            {
                services.AddSingleton<ICompensationSessionFinalizedPublisher>(publisher);
            });
            await using var scope = sp.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<TestBiDbContext>();
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();
            publisher.Db = db;

            var middleware = new RequestTransactionMiddleware(async ctx =>
            {
                db.DomainCruds.Add(new DomainCrud { Id = Guid.NewGuid(), Name = "t", Code = "T", TableName = "T" });
                await db.SaveChangesAsync();

                // CompensatableApiActionFilter'ın next() içinde bırakacağı işareti simüle ediyoruz.
                ctx.Items[CompensatableApiActionFilter.PendingFinalizeEventKey] = (sessionId, true);
            });

            var ctx = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
            ctx.Request.Method = "POST";
            ctx.Request.Path = "/api/mutation";

            await middleware.InvokeAsync(ctx, NullLogger<RequestTransactionMiddleware>.Instance);

            publisher.Calls.Should().ContainSingle();
            publisher.Calls[0].SessionId.Should().Be(sessionId);
            publisher.Calls[0].Success.Should().BeTrue();
            // Asıl regresyon kontrolü: publish anında transaction zaten commit edilmiş olmalı
            // (eski davranışta bu publish next() içinde, yani commit'ten ÖNCE çalışıyordu).
            publisher.Calls[0].TransactionWasAlreadyCommitted.Should().BeTrue();
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
    public async Task Rollback_requested_path_publishes_finalized_event_with_success_false_after_rollback()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bium-finalize-{Guid.NewGuid():N}.db");
        var correlation = new TestCorrelationContextProvider();
        var sessionId = Guid.NewGuid();
        var publisher = new RecordingCompensationSessionFinalizedPublisher();

        try
        {
            await using var sp = BiUMServiceFactory.BuildSqlite(correlation, path, services =>
            {
                services.AddSingleton<ICompensationSessionFinalizedPublisher>(publisher);
            });
            await using var scope = sp.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<TestBiDbContext>();
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();
            publisher.Db = db;

            var middleware = new RequestTransactionMiddleware(async ctx =>
            {
                db.DomainCruds.Add(new DomainCrud { Id = Guid.NewGuid(), Name = "t", Code = "T", TableName = "T" });
                await db.SaveChangesAsync();

                ctx.Items[ApiResponseTransactionRollbackFilter.RollbackRequestedKey] = true;
                ctx.Items[CompensatableApiActionFilter.PendingFinalizeEventKey] = (sessionId, false);
            });

            var ctx = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
            ctx.Request.Method = "POST";
            ctx.Request.Path = "/api/mutation";

            await middleware.InvokeAsync(ctx, NullLogger<RequestTransactionMiddleware>.Instance);

            publisher.Calls.Should().ContainSingle();
            publisher.Calls[0].SessionId.Should().Be(sessionId);
            publisher.Calls[0].Success.Should().BeFalse();
            publisher.Calls[0].TransactionWasAlreadyCommitted.Should().BeTrue();
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
    public async Task No_pending_event_key_means_publisher_is_never_called()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bium-finalize-{Guid.NewGuid():N}.db");
        var correlation = new TestCorrelationContextProvider();
        var publisher = new RecordingCompensationSessionFinalizedPublisher();

        try
        {
            await using var sp = BiUMServiceFactory.BuildSqlite(correlation, path, services =>
            {
                services.AddSingleton<ICompensationSessionFinalizedPublisher>(publisher);
            });
            await using var scope = sp.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<TestBiDbContext>();
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();
            publisher.Db = db;

            var middleware = new RequestTransactionMiddleware(_ => Task.CompletedTask);

            var ctx = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
            ctx.Request.Method = "POST";
            ctx.Request.Path = "/api/mutation";

            await middleware.InvokeAsync(ctx, NullLogger<RequestTransactionMiddleware>.Instance);

            publisher.Calls.Should().BeEmpty();
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

    private sealed class RecordingCompensationSessionFinalizedPublisher : ICompensationSessionFinalizedPublisher
    {
        public TestBiDbContext? Db { get; set; }

        public List<(Guid SessionId, bool Success, bool TransactionWasAlreadyCommitted)> Calls { get; } = [];

        public Task PublishAsync(Guid compensationSessionId, bool success, CancellationToken cancellationToken = default)
        {
            Calls.Add((compensationSessionId, success, Db?.Database.CurrentTransaction is null));

            return Task.CompletedTask;
        }
    }
}