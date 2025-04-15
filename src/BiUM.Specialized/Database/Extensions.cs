using AutoMapper;
using BiUM.Specialized.Common.API;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace BiUM.Specialized.Database;

public static class Extensions
{
    public static IServiceCollection AddDatabase<TDbContext, TDbContextInitialiser>(
        this IServiceCollection services, IConfiguration configuration
    )
        where TDbContext : DbContext
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

        services.AddScoped(typeof(IDbContextInitialiser), typeof(TDbContextInitialiser));

        return services;
    }

    [Obsolete("Obsuleted, use ToPaginatedListAsync or ToListAsync", true)]
    public static async Task<PaginatedApiResponse<TDestination>> ToPaginatedListAsync<TDestination>(this IQueryable<TDestination> queryable, int? pageStart = 0, int? pageSize = 10, CancellationToken cancellationToken = default) where TDestination : class
    {
        var query = queryable.AsNoTracking();

        var _pageStart = !pageStart.HasValue || pageStart.Value < 0 ? 0 : pageStart.Value;
        var _pageSize = !pageSize.HasValue || pageSize.Value < 0 ? 10 : pageSize.Value;

        return new PaginatedApiResponse<TDestination>(
            items: await query.Skip(_pageStart).Take(_pageSize).ToListAsync(cancellationToken),
            count: await query.CountAsync(cancellationToken),
            pageNumber: (_pageStart == 0 ? 0 : _pageStart / _pageSize) + 1,
            pageSize: _pageSize
        );
    }

    public static async Task<PaginatedApiResponse<TDestination>> ToPaginatedListAsync<TSource, TDestination>(
        this IQueryable<TSource> queryable,
        IMapper mapper,
        int? pageStart = 0,
        int? pageSize = 10,
        CancellationToken cancellationToken = default
    )
        where TSource : class
        where TDestination : class
    {
        var query = queryable.AsNoTracking();

        var _pageStart = !pageStart.HasValue || pageStart.Value < 0 ? 0 : pageStart.Value;
        var _pageSize = !pageSize.HasValue || pageSize.Value < 0 ? 10 : pageSize.Value;

        var items = mapper.Map<List<TDestination>>(await query.Skip(_pageStart).Take(_pageSize).ToListAsync(cancellationToken));

        return new PaginatedApiResponse<TDestination>(
            items: items,
            count: await query.CountAsync(cancellationToken),
            pageNumber: (_pageStart == 0 ? 0 : _pageStart / _pageSize) + 1,
            pageSize: _pageSize
        );
    }

    public static async Task<IList<TDestination>> ToListAsync<TSource, TDestination>(
        this IQueryable<TSource> queryable,
        IMapper mapper,
        CancellationToken cancellationToken = default
    )
        where TSource : class
        where TDestination : class
    {
        var query = queryable.AsNoTracking();

        var items = mapper.Map<IList<TDestination>>(await query.ToListAsync(cancellationToken));

        return items;
    }
}