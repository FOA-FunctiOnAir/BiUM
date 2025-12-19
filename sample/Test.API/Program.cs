using BiUM.Test.Application;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCoreServices(Assembly.GetExecutingAssembly());
builder.Services.AddInfrastructureServices(builder.Host, builder.Configuration);
builder.Services.AddSpecializedServices(builder.Configuration);

builder.Services.AddDomainAPIServices();
builder.Services.AddDomainApplicationServices(builder.Configuration);
await builder.Services.AddDomainInfrastructureServices(builder.Configuration);

builder.WebHost.AddSpecializedWebHost(builder.Services, builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}

await app.Services.SyncAll();

app.AddCoreApps();
app.AddInfrastructureApps();
app.AddSpecializedApps();

app.AddDomainInfrastructureApps();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

app.MapRazorPages();

app.MapFallbackToFile("index.html");

await app.RunAsync();