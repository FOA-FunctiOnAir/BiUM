using BiUM.Infrastructure.Common.Models;

namespace BiUM.Specialized.Common.Models;

public static class Extensions
{
    public static ITranslationBaseEntity ToTranslationEntity(this BaseTranslationDto translation, Guid recordId, string columnName)
    {
        return new TranslationBaseEntity()
        {
            Id = translation.Id,
            RecordId = recordId,
            Column = columnName,
            LanguageId = translation.LanguageId,
            Translation = translation.Translation,
        };
    }
}