using BiUM.Core.Common.Configs;
using BiUM.Core.Extensions;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;

namespace BiUM.Specialized.Database;

public static class EfMigrationsAssemblyResolver
{
    public static string? GetMigrationsAssemblyName(IConfiguration configuration, bool bolt)
    {
        var domain = configuration.GetSection("BiAppOptions")["Domain"];
        var databaseType = configuration.GetDatabaseType();

        if (string.IsNullOrWhiteSpace(domain) || string.IsNullOrWhiteSpace(databaseType))
        {
            return null;
        }

        if (databaseType is not (DatabaseTypes.PostgreSQL or DatabaseTypes.MSSQL))
        {
            return null;
        }

        var providerSuffix = databaseType == DatabaseTypes.PostgreSQL ? "Postgres" : "Mssql";
        var boltSuffix = bolt ? ".Bolt" : string.Empty;

        return $"BiApp.{domain}.Migrations.{providerSuffix}{boltSuffix}";
    }

    public static bool ShouldUseMigrations(IConfiguration configuration, bool bolt)
    {
        var assemblyName = GetMigrationsAssemblyName(configuration, bolt);

        if (string.IsNullOrWhiteSpace(assemblyName))
        {
            return false;
        }

        var assembly = TryLoadAssembly(assemblyName);

        return assembly is not null && HasMigrationClasses(assembly);
    }

    public static string? GetActiveMigrationsAssemblyName(IConfiguration configuration, bool bolt)
    {
        var assemblyName = GetMigrationsAssemblyName(configuration, bolt);

        if (string.IsNullOrWhiteSpace(assemblyName))
        {
            return null;
        }

        return TryLoadAssembly(assemblyName) is null ? null : assemblyName;
    }

    private static Assembly? TryLoadAssembly(string assemblyName)
    {
        try
        {
            return Assembly.Load(new AssemblyName(assemblyName));
        }
        catch (FileNotFoundException)
        {
            return null;
        }
        catch (FileLoadException)
        {
            return null;
        }
    }

    private static bool HasMigrationClasses(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes().Any(type => !type.IsAbstract && typeof(Migration).IsAssignableFrom(type));
        }
        catch (ReflectionTypeLoadException ex) when (ex.Types is not null)
        {
            return ex.Types.Any(type => type is not null && !type.IsAbstract && typeof(Migration).IsAssignableFrom(type));
        }
    }
}