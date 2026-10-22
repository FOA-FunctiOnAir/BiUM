using BiUM.Contract.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BiUM.Specialized.Services.ServiceCatalog;

internal static class ServiceCatalogTypeDescriber
{
    private const int MaxDepth = 4;

    private static readonly NullabilityInfoContext NullabilityContext = new();

    public static string ToPropertyName(string name) => JsonNamingPolicy.CamelCase.ConvertName(name);

    public static ServiceCatalogPropertyDto Describe(string name, Type type, string source, bool isNullable)
    {
        return Describe(name, type, source, isNullable, 0, []);
    }

    public static List<ServiceCatalogPropertyDto> DescribeMembers(Type type, string source)
    {
        return DescribeMembers(type, source, 0, []);
    }

    public static (Type? ValueType, bool IsPaginated) UnwrapResponseType(Type returnType)
    {
        var type = UnwrapTask(returnType);

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ActionResult<>))
        {
            type = type.GetGenericArguments()[0];
        }

        var isPaginated = false;

        for (var current = type; current is not null && current != typeof(object); current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(PaginatedApiResponse<>))
            {
                isPaginated = true;
            }

            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(ApiResponse<>))
            {
                return (current.GetGenericArguments()[0], isPaginated);
            }
        }

        return (null, false);
    }

    public static Type? GetCollectionElementType(Type type)
    {
        if (type == typeof(string) || type == typeof(byte[]))
        {
            return null;
        }

        if (type.IsArray)
        {
            return type.GetElementType();
        }

        if (!typeof(IEnumerable).IsAssignableFrom(type) || IsDictionary(type))
        {
            return null;
        }

        var enumerable = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
            ? type
            : type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        return enumerable?.GetGenericArguments()[0] ?? typeof(object);
    }

    private static ServiceCatalogPropertyDto Describe(string name, Type type, string source, bool isNullable, int depth, HashSet<Type> visited)
    {
        var elementType = GetCollectionElementType(type);
        var isArray = elementType is not null;
        var valueType = elementType ?? type;
        var underlying = Nullable.GetUnderlyingType(valueType);

        if (underlying is not null)
        {
            valueType = underlying;
            isNullable = isNullable || !isArray;
        }

        var property = new ServiceCatalogPropertyDto
        {
            Name = ToPropertyName(name),
            Source = source,
            ClrType = MapClrType(valueType),
            EnumTypeName = valueType.IsEnum ? valueType.Name : null,
            IsArray = isArray,
            IsNullable = isNullable
        };

        if (property.ClrType == ServiceCatalogClrTypes.Object && !IsDictionary(valueType) && valueType != typeof(object))
        {
            property.Children = DescribeMembers(valueType, source, depth + 1, visited);
        }

        return property;
    }

    private static List<ServiceCatalogPropertyDto> DescribeMembers(Type type, string source, int depth, HashSet<Type> visited)
    {
        if (depth >= MaxDepth || !visited.Add(type))
        {
            return [];
        }

        var properties = type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0 && p.GetCustomAttribute<JsonIgnoreAttribute>() is null)
            .Select(p => Describe(
                p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? p.Name,
                p.PropertyType,
                source,
                IsNullable(p),
                depth,
                visited))
            .ToList();

        visited.Remove(type);

        return properties;
    }

    private static bool IsNullable(PropertyInfo property)
    {
        if (property.PropertyType.IsValueType)
        {
            return Nullable.GetUnderlyingType(property.PropertyType) is not null;
        }

        return NullabilityContext.Create(property).ReadState == NullabilityState.Nullable;
    }

    private static string MapClrType(Type type)
    {
        if (type == typeof(Guid))
        {
            return ServiceCatalogClrTypes.Guid;
        }

        if (type == typeof(string) || type == typeof(char))
        {
            return ServiceCatalogClrTypes.String;
        }

        if (type == typeof(bool))
        {
            return ServiceCatalogClrTypes.Boolean;
        }

        if (type == typeof(byte) || type == typeof(short) || type == typeof(int))
        {
            return ServiceCatalogClrTypes.Integer;
        }

        if (type == typeof(long))
        {
            return ServiceCatalogClrTypes.Long;
        }

        if (type == typeof(decimal))
        {
            return ServiceCatalogClrTypes.Decimal;
        }

        if (type == typeof(double) || type == typeof(float))
        {
            return ServiceCatalogClrTypes.Double;
        }

        if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
        {
            return ServiceCatalogClrTypes.DateTime;
        }

        if (type == typeof(DateOnly))
        {
            return ServiceCatalogClrTypes.DateOnly;
        }

        if (type == typeof(TimeOnly) || type == typeof(TimeSpan))
        {
            return ServiceCatalogClrTypes.TimeOnly;
        }

        if (type.IsEnum)
        {
            return ServiceCatalogClrTypes.Enum;
        }

        if (type == typeof(byte[]))
        {
            return ServiceCatalogClrTypes.Binary;
        }

        if (typeof(IFormFile).IsAssignableFrom(type))
        {
            return ServiceCatalogClrTypes.File;
        }

        return ServiceCatalogClrTypes.Object;
    }

    private static bool IsDictionary(Type type) =>
        typeof(IDictionary).IsAssignableFrom(type) ||
        type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));

    private static Type UnwrapTask(Type type)
    {
        if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(Task<>) || type.GetGenericTypeDefinition() == typeof(ValueTask<>)))
        {
            return type.GetGenericArguments()[0];
        }

        return type;
    }
}