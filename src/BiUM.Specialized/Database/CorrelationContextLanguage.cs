using AutoMapper;
using AutoMapper.QueryableExtensions;
using BiUM.Contract.Models;
using BiUM.Core.Authorization;
using System;
using System.Linq;

namespace BiUM.Specialized.Database;

internal static class CorrelationContextLanguage
{
    private static ICorrelationContextAccessor? _accessor;

    public static void Configure(ICorrelationContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public static Guid Resolve(Guid? explicitLanguageId = null)
    {
        if (explicitLanguageId is Guid id && id != Guid.Empty)
        {
            return id;
        }

        return _accessor?.CorrelationContext?.LanguageId ?? CorrelationContext.DefaultLanguageId;
    }

    public static IQueryable<TDestination> ProjectToMapped<TSource, TDestination>(
        IQueryable<TSource> query,
        IMapper mapper,
        Guid? explicitLanguageId = null)
        where TSource : class
        where TDestination : class
    {
        var languageId = Resolve(explicitLanguageId);

        return query.ProjectTo<TDestination>(mapper.ConfigurationProvider, new { languageId });
    }
}