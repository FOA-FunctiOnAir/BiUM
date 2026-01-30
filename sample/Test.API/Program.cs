using BiUM.Core.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

#if DEBUG
builder.Configuration.OverrideAppLocalServices();
#endif

builder.ConfigureCoreServices();
builder.ConfigureInfrastructureServices();
builder.ConfigureSpecializedServices();

builder.Services.AddDomainApplicationServices(builder.Configuration);
builder.Services.AddDomainInfrastructureServices(builder.Configuration);

builder.ConfigureSpecializedHost();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}

await app.Services.SyncAll();

app.UseCore();
app.UseInfrastructure();
app.UseSpecialized();

app.AddDomainInfrastructureApps();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

app.MapRazorPages();

app.MapFallbackToFile("index.html");

await app.RunAsync();