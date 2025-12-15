using AutoMapper;
using BiUM.Core.Common.Configs;
using BiUM.Core.Database;
using BiUM.Core.Models;
using BiUM.Infrastructure.Common.Models;
using BiUM.Infrastructure.Services.Authorization;
using BiUM.Specialized.Common.Models;
using BiUM.Specialized.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BiUM.Specialized.Database;

public partial class BaseRepository : IBaseRepository
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDbContext _baseContext;

    public readonly ICorrelationContextProvider _correlationContextProvider;
    public readonly CorrelationContext? _correlationContext;
    public readonly ITranslationService _translationService;
    public readonly IMapper _mapper;

    public readonly BiAppOptions _biAppOptions;

    public BaseRepository(IServiceProvider serviceProvider, IDbContext baseContext)
    {
        _baseContext = baseContext;
        _serviceProvider = serviceProvider;

        _correlationContextProvider = _serviceProvider.GetRequiredService<ICorrelationContextProvider>();
        _translationService = _serviceProvider.GetRequiredService<ITranslationService>();
        _mapper = _serviceProvider.GetRequiredService<IMapper>();

        _biAppOptions = _serviceProvider.GetRequiredService<IOptions<BiAppOptions>>().Value;

        _correlationContext = _correlationContextProvider.Get();
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