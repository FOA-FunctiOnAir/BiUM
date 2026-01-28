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

public abstract partial class BaseRepository : IBaseRepository
{
    protected IDbContext DbContext { get; }

    protected CorrelationContext CorrelationContext { get; }
    protected ITranslationService TranslationService { get; }
    protected ILogger<BaseRepository> Logger { get; }
    protected IMapper Mapper { get; }

    protected BiAppOptions BiAppOptions { get; }

    protected BaseRepository(IServiceProvider serviceProvider, IDbContext dbContext)
    {
        DbContext = dbContext;

        var correlationContextProvider = serviceProvider.GetRequiredService<ICorrelationContextProvider>();

        CorrelationContext = correlationContextProvider.Get() ?? CorrelationContext.Empty;
        TranslationService = serviceProvider.GetRequiredService<ITranslationService>();
        Logger = serviceProvider.GetRequiredService<ILogger<BaseRepository>>();
        Mapper = serviceProvider.GetRequiredService<IMapper>();
        BiAppOptions = serviceProvider.GetRequiredService<IOptions<BiAppOptions>>().Value;
    }

    protected virtual Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return DbContext.SaveChangesAsync(cancellationToken);
    }

    protected virtual async Task SaveTranslations<TTranslationBaseEntity>(
        DbSet<TTranslationBaseEntity> dbSetTranslationEntity,
        Guid recordId,
        string columnName,
        IReadOnlyList<BaseTranslationDto> translations,
        CancellationToken cancellationToken)
        where TTranslationBaseEntity : class, ITranslationBaseEntity, new()
    {
        if (translations.Count == 0)
        {
            return;
        }

        foreach (var translation in translations)
        {
            if (translation._rowStatus == RowStatuses.New)
            {
                var applicationTranslation = translation.ToTranslationEntity<TTranslationBaseEntity>(recordId, columnName);

                dbSetTranslationEntity.Add(applicationTranslation);
            }
            else if (translation._rowStatus == RowStatuses.Edited)
            {
                var applicationTranslation = await dbSetTranslationEntity.FirstOrDefaultAsync(f => f.Id == translation.Id, cancellationToken);

                if (applicationTranslation is not null)
                {
                    applicationTranslation.Translation = translation.Translation;

                    dbSetTranslationEntity.Update(applicationTranslation);
                }
            }
            else if (translation._rowStatus == RowStatuses.Deleted)
            {
                var applicationTranslation = await dbSetTranslationEntity.FirstOrDefaultAsync(f => f.Id == translation.Id, cancellationToken);

                if (applicationTranslation is not null)
                {
                    dbSetTranslationEntity.Remove(applicationTranslation);
                }
            }
        }
    }
}