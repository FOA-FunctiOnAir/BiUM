using BiUM.Specialized.Database;
using BiUM.Specialized.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class ApplicationExtensions
{
    public static WebApplication UseSpecialized(this WebApplication app)
    {
        _ = app.UseMiddleware<RequestTransactionMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseMigrationsEndPoint();
        }

        app.UseStaticFiles();

        return app;
    }

    public static Task InitialiseDatabase(this IServiceProvider serviceProvider)
    {
        var initialiser = serviceProvider.GetRequiredService<IDbContextInitialiser>();

        return initialiser.InitialiseAsync();
    }

    public static Task SyncDatabase(this IServiceProvider serviceProvider)
    {
        var initialiser = serviceProvider.GetRequiredService<IDbContextInitialiser>();

        return initialiser.SeedAsync();
    }
}