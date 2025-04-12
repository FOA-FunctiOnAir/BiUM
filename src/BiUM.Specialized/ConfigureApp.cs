using BiUM.Specialized.Database;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class ConfigureApp
{
    public static IApplicationBuilder AddSpecializedApps(this IApplicationBuilder app, IWebHostEnvironment environment)
    {
        // Configure Serilog logging
        app.UseSerilogRequestLogging();

        // Configure the HTTP request pipeline.
        if (environment.IsDevelopment())
        {
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