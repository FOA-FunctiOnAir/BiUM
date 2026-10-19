using AutoMapper;
using AutoMapper.Internal;
using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Database;
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
        ApplyAuditFieldMapping();
        ApplyMappingsFromAssembly(typeof(IMapFrom<>).Assembly);
    }

    public MappingProfile(Assembly assembly)
    {
        ApplyAuditFieldMapping();
        ApplyMappingsFromAssembly(typeof(IMapFrom<>).Assembly);
        ApplyMappingsFromAssembly(assembly);
    }

    public MappingProfile(Assembly assembly, Assembly assembly2)
    {
        ApplyAuditFieldMapping();
        ApplyMappingsFromAssembly(typeof(IMapFrom<>).Assembly);
        ApplyMappingsFromAssembly(assembly);
        ApplyMappingsFromAssembly(assembly2);
    }

    public MappingProfile(Assembly assembly, Assembly assembly2, Assembly assembly3)
    {
        ApplyAuditFieldMapping();
        ApplyMappingsFromAssembly(typeof(IMapFrom<>).Assembly);
        ApplyMappingsFromAssembly(assembly);
        ApplyMappingsFromAssembly(assembly2);
        ApplyMappingsFromAssembly(assembly3);
    }

    // Attaches the CreatedAt/UpdatedAt AfterMap hook to every (IBaseEntity -> BaseDto) type map in
    // this profile via AutoMapper's internal-but-public ForAllMaps hook, instead of re-invoking
    // CreateMap for an already-registered pair -- doing that resets any ForMember configuration a
    // type's own custom Mapping(Profile) already set up (e.g. translated Name/NameTr), since this
    // AutoMapper version does not treat repeated CreateMap calls for the same pair as idempotent.
    private void ApplyAuditFieldMapping()
    {
        ((IProfileExpressionInternal)this).ForAllMaps((typeMap, expression) =>
        {
            if (!typeMap.DestinationType.IsAssignableTo(typeof(BaseDto)) ||
                !typeMap.SourceType.IsAssignableTo(typeof(IBaseEntity)))
            {
                return;
            }

            expression.AfterMap((src, dst) =>
            {
                if (src is not IBaseEntity entity || dst is not BaseDto dto)
                {
                    return;
                }

                dto.CreatedAt = CorrelationContextTimeZone.ConvertToLocal(entity.Created, entity.CreatedTime);
                dto.UpdatedAt = CorrelationContextTimeZone.ConvertToLocalOrNull(entity.Updated, entity.UpdatedTime);
            });
        });
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

            var interfaces = type.GetInterfaces().Where(t => HasInterface(t, mapFromType)).ToList();

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