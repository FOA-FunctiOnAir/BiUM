using BiUM.Specialized.Database;
using Microsoft.AspNetCore.Builder;
using Serilog;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class ConfigureApp
{
    public static IApplicationBuilder AddSpecializedApps(this IApplicationBuilder app)
    {
        var BiAppOrigins = "BiAppOrigins";

        // Configure Serilog logging
        //app.UseSerilogRequestLogging();
        //app.UseSerilogExceptionHandler();

        app.UseCors(BiAppOrigins);

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
            options.RoutePrefix = "swagger";
        });

        app.UseHealthChecks("/health");
        //app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }

    public static async Task SyncDatabase(this IServiceScope scope)
    {
        var initialiser = scope.ServiceProvider.GetRequiredService<IDbContextInitialiser>();

        await initialiser.InitialiseAsync();
        await initialiser.SeedAsync();
    }
}