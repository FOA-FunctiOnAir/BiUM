using BiUM.Infrastructure.Common.Models;

namespace BiUM.Specialized.Common.Models;

public static class Extensions
{
    public static TTranslationBaseEntity ToTranslationEntity<TTranslationBaseEntity>(
        this BaseTranslationDto translation,
        Guid recordId,
        string columnName)
        where TTranslationBaseEntity : class, ITranslationBaseEntity, new()
    {
        return new TTranslationBaseEntity
        {
            Id = translation.Id,
            RecordId = recordId,
            Column = columnName,
            LanguageId = translation.LanguageId,
            Translation = translation.Translation
        };
    }


    public static TranslationBaseEntity ToTranslationEntity(
        this BaseTranslationDto translation,
        Guid recordId,
        string columnName)
    {
        return translation.ToTranslationEntity<TranslationBaseEntity>(recordId, columnName);
    }
}