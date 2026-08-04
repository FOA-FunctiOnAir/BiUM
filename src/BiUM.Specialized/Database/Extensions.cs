using BiUM.Contract.Models.MessageBroker;
using BiUM.Infrastructure.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace BiUM.Specialized.Database;

public static partial class Extensions
{
    public static IServiceCollection AddDatabase<TDbContext, TDbContextInitialiser>(
        this IServiceCollection services, IConfiguration configuration
    )
        where TDbContext : DbContext, IDbContext
        where TDbContextInitialiser : class
    {
        Action<DbContextOptionsBuilder>? configureOptions = null;

        if (configuration.GetValue<string>("DatabaseType") == "InMemory")
        {
            configureOptions = options =>
                options.UseInMemoryDatabase("InMemoryDb");
        }
        else if (configuration.GetValue<string>("DatabaseType") == "MSSQL")
        {
            configureOptions = options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("MSSQL"),
                    sql =>
                    {
                        var mainMigrationsAssembly = EfMigrationsAssemblyResolver.GetActiveMigrationsAssemblyName(configuration, bolt: false);
                        if (mainMigrationsAssembly is not null)
                        {
                            _ = sql.MigrationsAssembly(mainMigrationsAssembly);
                        }
                        _ = sql.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(10),
                            errorNumbersToAdd: null);
                        _ = sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    });
        }
        else if (configuration.GetValue<string>("DatabaseType") == "PostgreSQL")
        {
            var connectionStringBuilder = new NpgsqlConnectionStringBuilder(configuration.GetConnectionString("PostgreSQL"))
            {
                Pooling = true,
                MinPoolSize = 0,
                MaxPoolSize = 100,
                KeepAlive = 30
            };

            configureOptions = options =>
                options.UseNpgsql(
                    connectionStringBuilder.ConnectionString,
                    npgsqlOptions =>
                    {
                        var mainMigrationsAssembly = EfMigrationsAssemblyResolver.GetActiveMigrationsAssemblyName(configuration, bolt: false);
                        if (mainMigrationsAssembly is not null)
                        {
                            _ = npgsqlOptions.MigrationsAssembly(mainMigrationsAssembly);
                        }
                        npgsqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(10),
                            errorCodesToAdd: null);
                        npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    });
        }

        if (configureOptions is not null)
        {
            services.AddDbContext<TDbContext>(configureOptions);

            // Compensatable entity'lerin erken/bağımsız commit'i için (CompensationEntityProcessor.Apply):
            // ambient request transaction'ından tamamen bağımsız, kendi connection'ını açan kısa ömürlü context'ler.
            services.AddDbContextFactory<TDbContext>(configureOptions);
        }

        services.AddScoped<IDbContext>(provider => provider.GetRequiredService<TDbContext>());
        services.AddScoped(typeof(IDbContextInitialiser), typeof(TDbContextInitialiser));

        services.AddDatabaseDeveloperPageExceptionFilter();

        services.AddHealthChecks().AddDbContextCheck<TDbContext>();

        return services;
    }

    public static IQueryable<T> OrderQuery<T>(
        this IQueryable<T> source,
        IBaseQuery baseQuery)
    {
        if (!baseQuery.HasExplicitSortBy() && source.IsAlreadyOrdered())
        {
            return source;
        }

        var (_sortBy, _sortDirection, _, _) = baseQuery.GetQueryParameters();

        var query = source.OrderByProperty(_sortBy, _sortDirection);

        return query;
    }

    public static IQueryable<T> OrderPaginatedQuery<T>(
        this IQueryable<T> source,
        IBaseQuery baseQuery)
    {
        var (_sortBy, _sortDirection, _pageStart, _pageSize) = baseQuery.GetQueryParameters();

        var orderedSource = !baseQuery.HasExplicitSortBy() && source.IsAlreadyOrdered()
            ? source
            : source.OrderByProperty(_sortBy, _sortDirection);

        var query = orderedSource.Skip(_pageStart).Take(_pageSize);

        return query;
    }

    private static bool HasExplicitSortBy(this IBaseQuery baseQuery) =>
        !string.IsNullOrEmpty(baseQuery?.SortBy);

    private static bool IsAlreadyOrdered<T>(this IQueryable<T> source)
    {
        var expression = source.Expression;

        while (expression is MethodCallExpression methodCall)
        {
            if (methodCall.Method.DeclaringType == typeof(Queryable) &&
                (methodCall.Method.Name == nameof(Queryable.OrderBy) ||
                 methodCall.Method.Name == nameof(Queryable.OrderByDescending)))
            {
                return true;
            }

            if (methodCall.Arguments.Count == 0)
            {
                break;
            }

            expression = methodCall.Arguments[0];
        }

        return false;
    }

    public static IQueryable<T> OrderByProperty<T>(
        this IQueryable<T> source,
        string propertyName,
        SortDirection sortDirection)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var property = Expression.PropertyOrField(parameter, propertyName);

        if (property is null)
        {
            return source;
        }

        var lambda = Expression.Lambda(property, parameter);

        var method = sortDirection == SortDirection.Desc ? "OrderByDescending" : "OrderBy";

        var result = Expression.Call(
            typeof(Queryable),
            method,
            [typeof(T), property.Type],
            source.Expression,
            Expression.Quote(lambda));

        return source.Provider.CreateQuery<T>(result);
    }

    private static (string sortBy, SortDirection sortDirection, int PageStart, int PageSize) GetQueryParameters(this IBaseQuery baseQuery)
    {
        if (baseQuery is null)
        {
            return (nameof(IBaseEntity.Created), SortDirection.Desc, 0, 10);
        }

        var sortBy = !string.IsNullOrEmpty(baseQuery.SortBy) ? baseQuery.SortBy : nameof(IBaseEntity.Created);
        var sortDirection = baseQuery.SortDirection ?? SortDirection.Desc;
        var pageStart = !baseQuery.PageStart.HasValue || baseQuery.PageStart.Value < 0 ? 0 : baseQuery.PageStart.Value;
        var pageSize = !baseQuery.PageSize.HasValue || baseQuery.PageSize.Value < 0 ? 10 : baseQuery.PageSize.Value;

        return (sortBy, sortDirection, pageStart, pageSize);
    }
}