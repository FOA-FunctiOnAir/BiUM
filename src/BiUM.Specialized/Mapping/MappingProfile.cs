using System;
using System.Linq;
using System.Reflection;

namespace BiUM.Specialized.Mapping;

public class MappingProfile : AutoMapper.Profile
{
    public MappingProfile()
    {
        ApplyMappingsFromAssembly(typeof(IMapFrom<>).Assembly);
    }

    public MappingProfile(Assembly assembly)
    {
        ApplyMappingsFromAssembly(typeof(IMapFrom<>).Assembly);
        ApplyMappingsFromAssembly(assembly);
    }

    public MappingProfile(Assembly assembly, Assembly assembly2)
    {
        ApplyMappingsFromAssembly(typeof(IMapFrom<>).Assembly);
        ApplyMappingsFromAssembly(assembly);
        ApplyMappingsFromAssembly(assembly2);
    }

    public MappingProfile(Assembly assembly, Assembly assembly2, Assembly assembly3)
    {
        ApplyMappingsFromAssembly(typeof(IMapFrom<>).Assembly);
        ApplyMappingsFromAssembly(assembly);
        ApplyMappingsFromAssembly(assembly2);
        ApplyMappingsFromAssembly(assembly3);
    }

    public void CreateAssemblyMap<TAssembly>()
    {
        var assembly = typeof(TAssembly).Assembly;

        ApplyMappingsFromAssembly(assembly);
    }

    private void ApplyMappingsFromAssembly(Assembly assembly)
    {
        var mapFromType = typeof(IMapFrom<>);

        var mappingMethodName = nameof(IMapFrom<>.Mapping);

        var types = assembly.GetExportedTypes().Where(t => t.GetInterfaces().Any(x => HasInterface(x, mapFromType)));

        var argumentTypes = new[] { typeof(AutoMapper.Profile) };

        foreach (var type in types)
        {
            if (type.Name == "BaseForValuesDto`1")
            {
                continue;
            }

            var instance = Activator.CreateInstance(type);

            var methodInfo = type.GetMethod(mappingMethodName);

            if (methodInfo is not null)
            {
                methodInfo.Invoke(instance, [this]);
            }
            else
            {
                var interfaces = type.GetInterfaces().Where(t => HasInterface(t, mapFromType));

                foreach (var @interface in interfaces)
                {
                    var interfaceMethodInfo = @interface.GetMethod(mappingMethodName, argumentTypes);

                    interfaceMethodInfo?.Invoke(instance, [this]);
                }
            }
        }
    }

    private static bool HasInterface(Type type, Type mapFromType)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == mapFromType;
    }
}

public class MappingProfile<TAssembly> : MappingProfile
{
    public MappingProfile() : base(typeof(TAssembly).Assembly)
    {
    }
}

public class MappingProfile<TAssembly, TAssembly2> : MappingProfile
{
    public MappingProfile() : base(typeof(TAssembly).Assembly, typeof(TAssembly2).Assembly)
    {
    }
}

public class MappingProfile<TAssembly, TAssembly2, TAssembly3> : MappingProfile
{
    public MappingProfile() : base(typeof(TAssembly).Assembly, typeof(TAssembly2).Assembly, typeof(TAssembly3).Assembly)
    {
    }
}
