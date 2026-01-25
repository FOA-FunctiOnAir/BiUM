using AutoMapper;
using BiUM.Contract.Models;
using BiUM.Core.Authorization;
using BiUM.Core.Common.Configs;
using BiUM.Core.Database;
using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Common.Models;
using BiUM.Specialized.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Database;

public partial class BaseRepository : IBaseRepository
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDbContext _baseContext;
    private readonly ICorrelationContextProvider _correlationContextProvider;

    public readonly CorrelationContext CorrelationContext;
    public readonly ITranslationService TranslationService;
    public readonly ILogger<BaseRepository> Logger;
    public readonly IMapper Mapper;

    public readonly BiAppOptions BiAppOptions;

    public BaseRepository(IServiceProvider serviceProvider, IDbContext baseContext)
    {
        _baseContext = baseContext;
        _serviceProvider = serviceProvider;

        _correlationContextProvider = _serviceProvider.GetRequiredService<ICorrelationContextProvider>();
        TranslationService = _serviceProvider.GetRequiredService<ITranslationService>();
        Logger = _serviceProvider.GetRequiredService<ILogger<BaseRepository>>();
        Mapper = _serviceProvider.GetRequiredService<IMapper>();

        BiAppOptions = _serviceProvider.GetRequiredService<IOptions<BiAppOptions>>().Value;

        CorrelationContext = _correlationContextProvider.Get() ?? CorrelationContext.Empty;
    }

    public virtual Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _baseContext.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task SaveTranslations<TTranslationBaseEntity>(
        DbSet<TTranslationBaseEntity> dbSetTranslationEntity,
        Guid recordId,
        string columnName,
        IReadOnlyList<BaseTranslationDto> translations,
        CancellationToken cancellationToken)
        where TTranslationBaseEntity : class, ITranslationBaseEntity, new()
    {
        if (translations is null || translations.Count == 0)
        {
            return;
        }

        foreach (var translation in translations)
        {
            if (translation._rowStatus == RowStatuses.New)
            {
                var applicationTranslation = translation.ToTranslationEntity<TTranslationBaseEntity>(recordId, columnName);

                _ = dbSetTranslationEntity.Add(applicationTranslation);
            }
            else if (translation._rowStatus == RowStatuses.Edited)
            {
                var applicationTranslation = await dbSetTranslationEntity.FirstOrDefaultAsync(f => f.Id == translation.Id, cancellationToken);

                if (applicationTranslation is not null)
                {
                    applicationTranslation.Translation = translation.Translation;

                    _ = dbSetTranslationEntity.Update(applicationTranslation);
                }
            }
            else if (translation._rowStatus == RowStatuses.Deleted)
            {
                var applicationTranslation = await dbSetTranslationEntity.FirstOrDefaultAsync(f => f.Id == translation.Id, cancellationToken);

                if (applicationTranslation is not null)
                {
                    _ = dbSetTranslationEntity.Remove(applicationTranslation);
                }
            }
        }
    }
}