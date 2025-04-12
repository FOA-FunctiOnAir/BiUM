using BiUM.Core.Caching.Redis;
using BiUM.Core.MessageBroker.RabbitMQ;
using BiUM.Infrastructure.Services.Caching.Redis;
using BiUM.Infrastructure.Services.MessageBroker.RabbitMQ;
using BiUM.UseCases.RabbitMQ;

using System.Reflection;

var builder = WebApplication.CreateBuilder(args);
{
    builder.Services.AddCoreServices(Assembly.GetExecutingAssembly());
    builder.Services.AddInfrastructureServices(builder.Host, builder.Configuration);
    builder.Services.AddSpecializedServices(builder.Configuration);

    builder.Services.AddControllers();
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // builder.Services.AddScoped<IRedisClient, RedisClient>();
    // builder.Services.AddScoped<IRabbitMQClient, RabbitMQClient>();

    // builder.Services.AddHostedService<RabbitMQHostedService>();
    // builder.Services.AddHostedService<RabbitMQListener>();

    builder.Services.AddRazorPages();
}

var app = builder.Build();
{
    app.AddCoreApps();
    app.AddInfrastructureApps();
    app.AddSpecializedApps(app.Environment);

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseExceptionHandler();
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller}/{action=Index}/{id?}");

    app.MapRazorPages();

    app.MapFallbackToFile("index.html");

    app.Run();
}