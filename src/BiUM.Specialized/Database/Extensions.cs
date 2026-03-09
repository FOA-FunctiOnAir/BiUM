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
        if (configuration.GetValue<string>("DatabaseType") == "InMemory")
        {
            services.AddDbContext<TDbContext>(options =>
                options.UseInMemoryDatabase("InMemoryDb"));
        }
        else if (configuration.GetValue<string>("DatabaseType") == "MSSQL")
        {
            services.AddDbContext<TDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("MSSQL"),
                    builder => builder.MigrationsAssembly(typeof(TDbContext).Assembly.FullName)));
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

            services.AddDbContext<TDbContext>(options =>
                options.UseNpgsql(
                    connectionStringBuilder.ConnectionString,
                    npgsqlOptions =>
                    {
                        npgsqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(10),
                            errorCodesToAdd: null);
                    }));
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
        var (_sortBy, _sortDirection, _, _) = baseQuery.GetQueryParameters();

        var query = source.OrderByProperty(_sortBy, _sortDirection);

        return query;
    }

    public static IQueryable<T> OrderPaginatedQuery<T>(
        this IQueryable<T> source,
        IBaseQuery baseQuery)
    {
        var (_sortBy, _sortDirection, _pageStart, _pageSize) = baseQuery.GetQueryParameters();

        var query = source.OrderByProperty(_sortBy, _sortDirection).Skip(_pageStart).Take(_pageSize);

        return query;
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