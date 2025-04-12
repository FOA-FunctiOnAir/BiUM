using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BiUM.Infrastructure.Common.Models;

public class TranslationBaseEntity : BaseAuditableEntity
{
    [Required]
    [Column("ID", Order = 1)]
    public override Guid Id { get; set; }

    [Required]
    [Column("RECORD_ID", Order = 2)]
    public Guid RecordId { get; set; }

    [Required]
    [Column("COLUMN", Order = 3)]
    public string Column { get; set; }

    [Required]
    [Column("LANGUAGE_ID", Order = 4)]
    public Guid LanguageId { get; set; }

    [Column("TRANSLATION", Order = 5)]
    public string? Translation { get; set; }

    public TranslationBaseEntity() : base()
    {
        Id = Guid.NewGuid();
        Column = string.Empty;
    }
}