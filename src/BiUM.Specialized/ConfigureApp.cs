using BiUM.Specialized.Database;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class ConfigureApp
{
    public static WebApplication AddSpecializedApps(this WebApplication app)
    {
        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapGrpcReflectionService();
            app.UseDeveloperExceptionPage();
        }
        else
        {
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();

            // app.UseHttpsRedirection();
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

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
            options.RoutePrefix = "swagger";
        });

        app.UseHealthChecks("/health");

        app.UseRouting();

        app.MapGet("/version", () =>
        {
            var version = Environment.GetEnvironmentVariable("APP_VERSION") ?? "unknown";

            return Results.Ok(new { version });
        });

        app.UseStaticFiles();

        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }

    public static async Task InitialiseDatabase(this IServiceScope scope)
    {
        var initialiser = scope.ServiceProvider.GetRequiredService<IDbContextInitialiser>();

        await initialiser.InitialiseAsync();
    }

    public static async Task SyncDatabase(this IServiceScope scope)
    {
        var initialiser = scope.ServiceProvider.GetRequiredService<IDbContextInitialiser>();

        await initialiser.SeedAsync();
    }
}