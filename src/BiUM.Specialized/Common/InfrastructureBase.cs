using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Common.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Common;

public abstract partial class InfrastructureBase : SpecializedBase
{
    protected InfrastructureBase(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    protected virtual async Task SaveTranslations<TTranslationBaseEntity>(
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