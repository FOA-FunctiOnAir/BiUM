using BiUM.Specialized.Database;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

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
            app.UseExceptionHandler(o => { });
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseCors(BiUM.Specialized.Consts.Application.BiAppOrigins);

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
            options.RoutePrefix = "swagger";
        });

        app.UseHealthChecks("/health");
        app.MapGet("/version", () =>
        {
            var version = Environment.GetEnvironmentVariable("APP_VERSION") ?? "unknown";

            return Results.Ok(new { version });
        });

        //app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

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