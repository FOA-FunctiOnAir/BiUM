using System;
using System.ComponentModel.DataAnnotations.Schema;

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


    [ForeignKey("TranslationId")]
    public virtual DomainTranslation? DomainTranslation { get; set; }
}