using AutoMapper;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace BiUM.Specialized.Mapping;

public class MappingProfile : Profile
{
    // L-4: compiled Func<object> per type replaces Activator.CreateInstance reflection on every mapping scan.
    private static readonly ConcurrentDictionary<Type, Func<object>> _instanceFactoryCache = new();

    private static object CreateInstance(Type type)
        => _instanceFactoryCache.GetOrAdd(type, static t =>
        {
            var ctor = t.GetConstructor(Type.EmptyTypes);
            if (ctor is not null)
            {
                return Expression.Lambda<Func<object>>(Expression.Convert(Expression.New(ctor), typeof(object))).Compile();
            }

            return () => System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(t);
        })();

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

        var argumentTypes = new[] { typeof(Profile) };

        foreach (var type in types)
        {
            if (type.Name == "BaseForValuesDto`1")
            {
                continue;
            }

            var methodInfo = type.GetMethod(
                mappingMethodName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy);

            if (methodInfo is not null)
            {
                if (methodInfo.IsStatic)
                {
                    methodInfo.Invoke(null, [this]);
                }
                else
                {
                    var instance = CreateInstance(type);

                    methodInfo.Invoke(instance, [this]);
                }

                continue;
            }

            var instanceForInterface = CreateInstance(type);

            var interfaces = type.GetInterfaces().Where(t => HasInterface(t, mapFromType));

            foreach (var @interface in interfaces)
            {
                var interfaceMethodInfo = @interface.GetMethod(mappingMethodName, argumentTypes);

                interfaceMethodInfo?.Invoke(instanceForInterface, [this]);
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