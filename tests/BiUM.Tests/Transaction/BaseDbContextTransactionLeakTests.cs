using BiUM.Specialized.Database;
using BiUM.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace BiUM.Tests.Transaction;

public sealed class BaseDbContextTransactionLeakTests
{
    [Fact]
    public async Task Dispose_without_open_transaction_does_not_log()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bium-leak-{Guid.NewGuid():N}.db");
        var correlation = new TestCorrelationContextProvider();
        var capturingLogger = new CapturingLogger<BaseDbContext>();

        try
        {
            await using var sp = BiUMServiceFactory.BuildSqlite(correlation, path, services =>
            {
                services.AddSingleton<ILogger<BaseDbContext>>(capturingLogger);
            });

            await using (var scope = sp.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TestBiDbContext>();
                await db.Database.EnsureDeletedAsync();
                await db.Database.EnsureCreatedAsync();
            }

            capturingLogger.ErrorMessages.Should().BeEmpty();
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
    public async Task Dispose_with_leaked_open_transaction_logs_error()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bium-leak-{Guid.NewGuid():N}.db");
        var correlation = new TestCorrelationContextProvider();
        var capturingLogger = new CapturingLogger<BaseDbContext>();

        try
        {
            await using var sp = BiUMServiceFactory.BuildSqlite(correlation, path, services =>
            {
                services.AddSingleton<ILogger<BaseDbContext>>(capturingLogger);
            });

            await using (var scope = sp.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TestBiDbContext>();
                await db.Database.EnsureDeletedAsync();
                await db.Database.EnsureCreatedAsync();

                await db.Database.BeginTransactionAsync();
            }

            capturingLogger.ErrorMessages.Should().ContainSingle();
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
    public async Task DisposeAsync_with_leaked_open_transaction_logs_error()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bium-leak-{Guid.NewGuid():N}.db");
        var correlation = new TestCorrelationContextProvider();
        var capturingLogger = new CapturingLogger<BaseDbContext>();

        try
        {
            await using var sp = BiUMServiceFactory.BuildSqlite(correlation, path, services =>
            {
                services.AddSingleton<ILogger<BaseDbContext>>(capturingLogger);
            });

            var scope = sp.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<TestBiDbContext>();
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();

            await db.Database.BeginTransactionAsync();

            await scope.DisposeAsync();

            capturingLogger.ErrorMessages.Should().ContainSingle();
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
    public async Task Dispose_after_commit_does_not_log()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bium-leak-{Guid.NewGuid():N}.db");
        var correlation = new TestCorrelationContextProvider();
        var capturingLogger = new CapturingLogger<BaseDbContext>();

        try
        {
            await using var sp = BiUMServiceFactory.BuildSqlite(correlation, path, services =>
            {
                services.AddSingleton<ILogger<BaseDbContext>>(capturingLogger);
            });

            await using (var scope = sp.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TestBiDbContext>();
                await db.Database.EnsureDeletedAsync();
                await db.Database.EnsureCreatedAsync();

                var transaction = await db.Database.BeginTransactionAsync();
                await transaction.CommitAsync();
            }

            capturingLogger.ErrorMessages.Should().BeEmpty();
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

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> ErrorMessages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Error)
            {
                ErrorMessages.Add(formatter(state, exception));
            }
        }
    }
}