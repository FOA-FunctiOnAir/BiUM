using BiUM.Specialized.Database;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Microsoft.Extensions.DependencyInjection;

public static class ConfigureApp
{
    public static WebApplication AddSpecializedApps(this WebApplication app)
    {
        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapGrpcReflectionService();
            app.UseDeveloperExceptionPage();
        }

        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();

                if (exceptionHandlerFeature?.Error is null) return;

                context.Response.StatusCode = 500;

                await context.Response.WriteAsync("An unexpected error occurred");

                var exception = exceptionHandlerFeature.Error;

                Log.Error(exception, "An unhandled exception occurred");
            });
        });

        AppDomain.CurrentDomain.UnhandledException +=
            (_, args) =>
            {
                if (args.ExceptionObject is Exception ex)
                {
                    Log.Fatal(ex, "An unhandled exception occurred");
                }
                else
                {
                    Log.Fatal("An unhandled exception occurred, but no Exception object was provided");
                }
            };

        TaskScheduler.UnobservedTaskException +=
            (_, args) =>
            {
                Log.Error(args.Exception, "An unobserved task exception occurred");

                args.SetObserved();
            };

        app.UseCors(BiUM.Specialized.Consts.Application.BiAppOrigins);

        app.UseRouting();

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
            options.RoutePrefix = "swagger";
        });

        app.UseHealthChecks("/health");

        app.MapGet("/version", () => Results.Ok(new VersionResult
        {
            Version = Environment.GetEnvironmentVariable("APP_VERSION") ?? "unknown"
        }));

        app.UseStaticFiles();

        return app;
    }

    public static Task InitialiseDatabase(this IServiceScope scope)
    {
        var initialiser = scope.ServiceProvider.GetRequiredService<IDbContextInitialiser>();

        return initialiser.InitialiseAsync();
    }

    public static Task SyncDatabase(this IServiceScope scope)
    {
        var initialiser = scope.ServiceProvider.GetRequiredService<IDbContextInitialiser>();

        return initialiser.SeedAsync();
    }

    private sealed class VersionResult
    {
        public required string Version { get; init; }
    }
}