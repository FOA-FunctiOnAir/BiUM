using AutoMapper;
using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Database;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace BiUM.Specialized.Mapping;

public class MappingProfile : Profile
{
    // L-4: compiled Func<object> per type replaces Activator.CreateInstance reflection on every mapping scan.
    private static readonly ConcurrentDictionary<Type, Func<object>> _instanceFactoryCache = new();

    private readonly List<(Type Source, Type Dest)> _auditMapTargets = [];

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
        ApplyGlobalMappings();
    }

    public MappingProfile(Assembly assembly)
    {
        ApplyMappingsFromAssembly(typeof(IMapFrom<>).Assembly);
        ApplyMappingsFromAssembly(assembly);
        ApplyGlobalMappings();
    }

    public MappingProfile(Assembly assembly, Assembly assembly2)
    {
        ApplyMappingsFromAssembly(typeof(IMapFrom<>).Assembly);
        ApplyMappingsFromAssembly(assembly);
        ApplyMappingsFromAssembly(assembly2);
        ApplyGlobalMappings();
    }

    public MappingProfile(Assembly assembly, Assembly assembly2, Assembly assembly3)
    {
        ApplyMappingsFromAssembly(typeof(IMapFrom<>).Assembly);
        ApplyMappingsFromAssembly(assembly);
        ApplyMappingsFromAssembly(assembly2);
        ApplyMappingsFromAssembly(assembly3);
        ApplyGlobalMappings();
    }

    public void CreateAssemblyMap<TAssembly>()
    {
        var assembly = typeof(TAssembly).Assembly;

        ApplyMappingsFromAssembly(assembly);
    }

    private void ApplyGlobalMappings()
    {
        foreach (var (source, dest) in _auditMapTargets)
        {
            CreateMap(source, dest).AfterMap((src, dst) =>
            {
                if (src is IBaseEntity entity && dst is BaseDto dto)
                {
                    dto.CreatedAt = CorrelationContextTimeZone.ConvertToLocal(entity.Created, entity.CreatedTime);
                    dto.UpdatedAt = CorrelationContextTimeZone.ConvertToLocalOrNull(entity.Updated, entity.UpdatedTime);
                }
            });
        }
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

            var interfaces = type.GetInterfaces().Where(t => HasInterface(t, mapFromType)).ToList();

            if (type.IsAssignableTo(typeof(BaseDto)))
            {
                foreach (var @interface in interfaces)
                {
                    var sourceType = @interface.GetGenericArguments()[0];

                    if (sourceType.IsAssignableTo(typeof(IBaseEntity)))
                    {
                        _auditMapTargets.Add((sourceType, type));
                    }
                }
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