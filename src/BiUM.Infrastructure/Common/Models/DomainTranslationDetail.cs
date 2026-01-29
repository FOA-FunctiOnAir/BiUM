using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BiUM.Infrastructure.Common.Models;

[Table("__TRANSLATION_DETAIL", Schema = "dbo")]
public class DomainTranslationDetail : BaseEntity
{
    [Column("TRANSLATION_ID")]
    public Guid TranslationId { get; set; }

    [Column("TEXT")]
    public required string Text { get; set; }

    [Column("LANGUAGE_ID")]
    public Guid LanguageId { get; set; }


    [ForeignKey(nameof(TranslationId))]
    [JsonIgnore]
    public DomainTranslation DomainTranslation { get; private set; } = null!;
}
